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
    private readonly IConfiguration configuration;

    private const string DemoModeText =
        "This is a demo instance of BetterNote.\n" +
        "Your secret was replaced with this text to prevent misuse.\n\n" +
        "Seeing this text on your own instance? Set environment variable 'DemoMode' to 'false'.";

    public APIController(IDatabase database,
        ILogger<APIController> logger,
        IConfiguration configuration)
    {
        this.database = database;
        this.logger = logger;
        this.configuration = configuration;
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
        
        logger.LogInformation("Secret [{SecretId}] retrieved", noteId);

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

        var textToEncrypt = !configuration.GetValue<bool>("DemoMode") ? request.Text : DemoModeText;
        
        using var msEncrypt = new MemoryStream();
        await using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        await using (var swEncrypt = new StreamWriter(csEncrypt, Encoding.UTF8))
        {
            await swEncrypt.WriteAsync(textToEncrypt);
        }

        var secretId = Guid.NewGuid().ToString("N");
        var encrypted = Convert.ToBase64String(msEncrypt.ToArray());
        var key = Convert.ToBase64String(randomAes.Key);
        var iv = Convert.ToBase64String(randomAes.IV);

        var expiryTimeSpan = GetExpiryTimeSpan();
        
        try
        {
            await database.StringSetAsync(secretId, encrypted);
            await database.KeyExpireAsync(secretId, expiryTimeSpan);
        }
        catch (RedisTimeoutException ex)
        {
            logger.LogError(ex, "Unable to reach Redis");
            throw new LogicException(ErrorCode.RedisIsOffline, "Unable to reach Redis database.");
        }
        
        logger.LogInformation("Created secret with ID [{SecretId}], will expire at ~[{ApproxExpiryDate}] UTC",
            secretId, DateTime.UtcNow.Add(expiryTimeSpan));
        
        return new BaseResponse<CreateResponse>
        {
            Success = true,
            Result = new CreateResponse
            {
                Id = secretId,
                Key = key,
                IV = iv,
                NoteExpirationInMinutes = (int) expiryTimeSpan.TotalMinutes
            }
        };
    }

    [NonAction]
    private TimeSpan GetExpiryTimeSpan()
    {
        var configExpiryMinutes = configuration.GetValue<int>("SecretExpiryInMinutes");

        if (configExpiryMinutes == 0)
        {
            logger.LogWarning("SecretExpiryInMinutes is not set! Default value of 12 hours will be used");
            return TimeSpan.FromHours(12);
        }

        return TimeSpan.FromMinutes(configExpiryMinutes);
    }
}
