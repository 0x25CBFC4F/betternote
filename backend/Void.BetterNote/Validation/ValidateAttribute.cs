using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Void.BetterNote.Validation;

/// <summary>
/// Validation attribute. Causes action to be processed by <see cref="ValidationActionFilter{TRequest,TValidator}"/>
/// </summary>
public class ValidateAttribute<TRequest> : ServiceFilterAttribute
    where TRequest : class
{
    public ValidateAttribute() : base(typeof(ValidationActionFilter<TRequest, IValidator<TRequest>>))
    {
        IsReusable = true;
    }
}

/// <inheritdoc cref="ValidateAttribute{TRequest}"/>
public class ValidateAttribute<TRequest, TValidator> : ServiceFilterAttribute
    where TRequest : class
    where TValidator : IValidator<TRequest>
{
    public ValidateAttribute() : base(typeof(ValidationActionFilter<TRequest, TValidator>))
    {
        IsReusable = true;
    }
}
