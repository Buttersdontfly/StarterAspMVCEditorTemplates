using System.ComponentModel.DataAnnotations;
using StarterAspMVCEditorTemplates.Identity;

namespace StarterAspMVCEditorTemplates.Models.Account;

public class LoginInputModel : IValidatableObject
{
    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Enter an email address in the form name@example.com.")]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Used only when AccountIdentityConventions.SignInWithEmail is false.
    /// The field is always present so that flipping that constant needs no edit
    /// here; the view decides which of the two to render.
    /// </summary>
    [Display(Name = "Username")]
    [UIHint("UserName")]
    public string? UserName { get; set; }

    [Display(Name = "Password")]
    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Keep me signed in")]
    public bool RememberMe { get; set; }

    /// <summary>
    /// Which identifier is required depends on the sign-in convention, and data
    /// annotations are fixed at compile time, so the rule lives here instead.
    ///
    /// Note that this is server-side only: unobtrusive client validation emits
    /// rules from attributes, so the browser will not pre-empt this one. The
    /// server still rejects it, which is what matters.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AccountIdentityConventions.SignInWithEmail)
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                yield return new ValidationResult("Enter your email address.", [nameof(Email)]);
            }
        }
        else if (string.IsNullOrWhiteSpace(UserName))
        {
            yield return new ValidationResult("Enter your username.", [nameof(UserName)]);
        }
    }
}
