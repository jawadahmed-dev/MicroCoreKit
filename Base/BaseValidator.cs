using FluentValidation;

namespace MicroCoreKit.Base;

public abstract class BaseValidator<T> : AbstractValidator<T> where T : class
{
    protected BaseValidator()
    {
        // Add common validation rules that can be reused across validators
        RuleFor(x => x).NotNull().WithMessage("The object cannot be null.");

        // Optional: Add default rules for common properties (e.g., strings)
        RuleFor(x => x as object).ChildRules(child =>
        {
            child.RuleFor(y => y.ToString()).MaximumLength(1000)
                .When(y => y != null && !string.IsNullOrEmpty(y.ToString()))
                .WithMessage("Property value exceeds maximum length of 1000 characters.");
        });
    }

    /// <summary>
    /// Adds a rule for a string property to ensure it’s not empty and within a maximum length.
    /// </summary>
    protected IRuleBuilderOptions<T, string> StringRule(string propertyName, int maxLength = 100, bool allowEmpty = false)
    {
        var rule = RuleFor(x => propertyName)
        .Must((x, _) =>
        {
            var value = (string)x.GetType().GetProperty(propertyName)?.GetValue(x);
            return !string.IsNullOrEmpty(value);
        })
        .WithMessage($"{propertyName} cannot be null or empty.");


        if (!allowEmpty)
        {
            rule.NotEmpty().WithMessage($"{propertyName} is required.");
        }

        return rule.MaximumLength(maxLength).WithMessage($"{propertyName} must not exceed {maxLength} characters.");
    }

    /// <summary>
    /// Adds a rule for an email property to ensure it’s a valid email address.
    /// </summary>
    protected void EmailRule(string propertyName)
    {
        RuleFor(x => x)
            .Custom((x, context) =>
            {
                var propertyInfo = typeof(T).GetProperty(propertyName);
                if (propertyInfo == null)
                {
                    context.AddFailure($"{propertyName} does not exist.");
                    return;
                }

                var value = propertyInfo.GetValue(x) as string;
                if (string.IsNullOrEmpty(value) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(value))
                {
                    context.AddFailure($"{propertyName} must be a valid email address.");
                }
            });
    }


}