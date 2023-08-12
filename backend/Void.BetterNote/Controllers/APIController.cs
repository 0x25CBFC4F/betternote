using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using Void.BetterNote.DTO;
using Void.BetterNote.Exceptions;
using Void.BetterNote.Validation;

namespace Void.BetterNote.Controllers;

[Controller]
[Route("/api/bn")]
public class APIController : ControllerBase
{
    private readonly IDatabase database;
    private readonly ILogger<APIController> logger;

    public APIController(IDatabase database, ILogger<APIController> logger)
    {
        this.database = database;
        this.logger = logger;
    }

    [HttpGet("{noteId}")]
    public async Task<BaseResponse<string>> GetNoteAsync(string noteId)
    {
        RedisValue encryptedNote;
        
        try
        {
            encryptedNote = await database.StringGetAsync(noteId);
        }
        catch (RedisTimeoutException ex)
        {
            logger.LogError(ex, "Unable to reach Redis");
            throw new LogicException(ErrorCode.RedisIsOffline, "Unable to reach Redis database.");
        }

        if (encryptedNote.IsNullOrEmpty || !encryptedNote.HasValue)
            throw new LogicException(ErrorCode.ValidationError, "Unknown note ID!");

        await database.KeyDeleteAsync(noteId);

        return new BaseResponse<string>
        {
            Success = true,
            Result = encryptedNote.ToString()
        };
    }
    
    [Validate<CreateRequest>]
    [HttpPost("create")]
    public async Task<BaseResponse<CreateResponse>> CreateNoteAsync([FromBody] CreateRequest request)
    {
        using var randomAes = Aes.Create();

        randomAes.Mode = CipherMode.CBC;
        randomAes.GenerateKey();
        
        // Raw-byte IV seems to be fine
        randomAes.GenerateIV();

        using var encryptor = randomAes.CreateEncryptor();

        using var msEncrypt = new MemoryStream();
        await using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        await using (var swEncrypt = new StreamWriter(csEncrypt, Encoding.UTF8))
        {
            await swEncrypt.WriteAsync(request.Text);
        }

        var randomId = Guid.NewGuid().ToString("N");
        var encrypted = Convert.ToBase64String(msEncrypt.ToArray());
        var key = Convert.ToBase64String(randomAes.Key);
        var iv = Convert.ToBase64String(randomAes.IV);

        try
        {
            await database.StringSetAsync(randomId, encrypted);
            await database.KeyExpireAsync(randomId, TimeSpan.FromHours(12));
        }
        catch (RedisTimeoutException ex)
        {
            logger.LogError(ex, "Unable to reach Redis");
            throw new LogicException(ErrorCode.RedisIsOffline, "Unable to reach Redis database.");
        }
        
        return new BaseResponse<CreateResponse>
        {
            Success = true,
            Result = new CreateResponse
            {
                Id = randomId,
                Key = key,
                IV = iv
            }
        };
    }
}
