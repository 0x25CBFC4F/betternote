using System.Net;
using Void.BetterNote.DTO;
using Void.BetterNote.Exceptions;

namespace Void.BetterNote.Middlewares;

/// <summary>
/// Simple request-pipeline wise exception handler.
/// </summary>
public class ExceptionMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionMiddleware> logger;

    public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger)
    {
        this.logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (LogicException lex)
        {
            // Known problem
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsJsonAsync(new BaseResponse
            {
                Success = false,
                Error = new Error(lex.ErrorCode, lex.Message)
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occured");
            
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsJsonAsync(new BaseResponse
            {
                Success = false,
                Error = new Error(ErrorCode.InternalError, "Internal server error occured.")
            });
        }
    }
}
