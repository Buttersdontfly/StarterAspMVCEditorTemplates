using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SamplePlainApp.Utilities;

/// <summary>
/// Validates that a date value is greater than or equal to another date value.
/// Supports DateOnly, DateTime, and DateTimeOffset.
/// </summary>
/// <param name="value">The value being validated.</param>
/// <param name="validationContext">The validation context.</param>
/// <returns>
/// <see cref="ValidationResult.Success"/> if valid; otherwise, a validation error.
/// </returns>
public class DateGreaterThanOrEqualToAttribute : ValidationAttribute
{
    private readonly string _comparisonProperty;

    public DateGreaterThanOrEqualToAttribute(string comparisonProperty)
    {
        if (string.IsNullOrWhiteSpace(comparisonProperty))
        {
            throw new ArgumentException(
                "Comparison property must be specified.",
                nameof(comparisonProperty));
        }

        _comparisonProperty = comparisonProperty;
    }

    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        var property = validationContext.ObjectType.GetProperty(
            _comparisonProperty,
            BindingFlags.Public |
            BindingFlags.Instance);

        if (property == null)
        {
            return new ValidationResult(
                $"Unknown property: {_comparisonProperty}");
        }

        var comparisonValue = property.GetValue(
            validationContext.ObjectInstance);

        if (value == null || comparisonValue == null)
        {
            return ValidationResult.Success;
        }

        var isValid = (value, comparisonValue) switch
        {
            (DateOnly endDate, DateOnly startDate)
                => endDate >= startDate,

            (DateTime endDate, DateTime startDate)
                => endDate >= startDate,

            (DateTimeOffset endDate, DateTimeOffset startDate)
                => endDate >= startDate,

            _ => throw new InvalidOperationException(
                $"The properties '{validationContext.MemberName}' and " +
                $"'{_comparisonProperty}' must both be of the same supported " +
                $"type: DateOnly, DateTime, or DateTimeOffset. " +
                $"Actual types are '{value.GetType().Name}' and " +
                $"'{comparisonValue.GetType().Name}'.")
        };

        if (!isValid)
        {
            return new ValidationResult(
                ErrorMessage ??
                $"The {validationContext.DisplayName} must be " +
                $"greater than or equal to the {_comparisonProperty}.");
        }

        return ValidationResult.Success;
    }
}