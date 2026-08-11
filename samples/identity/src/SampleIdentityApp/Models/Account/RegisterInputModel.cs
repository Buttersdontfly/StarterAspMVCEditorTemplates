using System.ComponentModel.DataAnnotations;
using SampleIdentityApp.Identity;

namespace SampleIdentityApp.Models.Account;

public class RegisterInputModel : IValidatableObject
{
    [Display(Name = "Email")]
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter an email address in the form name@example.com.")]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Collected only when AccountIdentityConventions.SignInWithEmail is false.
    /// Always present so that flipping the constant needs no edit here.
    /// </summary>
    [Display(Name = "Username")]
    [UIHint("UserName")]
    [StringLength(64, MinimumLength = 3,
        ErrorMessage = "A username must be between 3 and 64 characters.")]
    public string? UserName { get; set; }

    [Display(Name = "Password")]
    [Required(ErrorMessage = "Choose a password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Confirm password")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// See the note on LoginInputModel.Validate: the requirement depends on a
    /// constant, so it cannot be expressed as an attribute.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!AccountIdentityConventions.SignInWithEmail && string.IsNullOrWhiteSpace(UserName))
        {
            yield return new ValidationResult("Choose a username.", [nameof(UserName)]);
        }
    }
}
