using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using Void.BetterNote.DTO;
using Void.BetterNote.Exceptions;

namespace Void.BetterNote.Validation;

/// <summary>
/// Action filter that looks up <typeparamref name="TValidator"/> in DI and attempts to validate request model.
/// </summary>
public class ValidationActionFilter<TRequest, TValidator> : IAsyncActionFilter
    where TRequest : class
    where TValidator : IValidator<TRequest>
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionArguments.Values.FirstOrDefault(x => x is TRequest) is TRequest argument)
        {
            var validator = context.HttpContext.RequestServices.GetService<TValidator>();

            if (validator is not null)
            {
                var validationResult = await validator.ValidateAsync(argument);
            
                if (!validationResult.IsValid)
                    throw new LogicException(ErrorCode.ValidationError, validationResult.Errors.First().ErrorMessage);
            }
        }
        else
        {
            throw new LogicException(ErrorCode.InternalError);
        }

        await next();
    }
}