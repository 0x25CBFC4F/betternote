using FluentValidation;
using Void.BetterNote.DTO;

namespace Void.BetterNote.Validation.Validators;

public class CreateRequestValidator : AbstractValidator<CreateRequest>
{
    public CreateRequestValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Text cannot be empty!");
    }
}
