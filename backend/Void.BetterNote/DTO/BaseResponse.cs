namespace Void.BetterNote.DTO;

/// <summary>
/// Error codes
/// </summary>
public enum ErrorCode
{
    /// <summary>
    /// Something is missing
    /// </summary>
    Missing,
    
    /// <summary>
    /// Invalid request
    /// </summary>
    ValidationError,
    
    /// <summary>
    /// Redis is offline
    /// </summary>
    RedisIsOffline,
    
    /// <summary>
    /// Internal server error
    /// </summary>
    InternalError
}

/// <summary>
/// Request error.
/// </summary>
/// <param name="Code">Error code.</param>
/// <param name="Message">Error message, can be null.</param>
public record Error(ErrorCode Code, string? Message);

/// <summary>
/// Base response from the server.
/// </summary>
public class BaseResponse
{
    /// <summary>
    /// Success flag.
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Request error.
    /// </summary>
    public Error? Error { get; set; }
}

/// <inheritdoc cref="BaseResponse"/>
public class BaseResponse<T> : BaseResponse
{
    /// <summary>
    /// Optional response model.
    /// </summary>
    public T? Result { get; set; }
}
