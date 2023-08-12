using Void.BetterNote.DTO;

namespace Void.BetterNote.Exceptions;

public class LogicException : Exception
{
    public ErrorCode ErrorCode { get; }

    public LogicException(ErrorCode errorCode, string? message = null) : base(message)
    {
        ErrorCode = errorCode;
    }
}
