using System.ComponentModel.DataAnnotations;
using SampleIdentityApp.Identity;

namespace SampleIdentityApp.Models.Account;

public class LoginInputModel : IValidatableObject
{
    /// <summary>
    /// Nullable on purpose, and it matters.
    ///
    /// ASP.NET Core adds an IMPLICIT [Required] to every non-nullable reference
    /// type property. The login form renders either Email or UserName, never
    /// both, so the one that is not rendered posts nothing and fails that
    /// implicit rule -- with a message about a field the user was never shown.
    /// Making both nullable removes the implicit requirement and leaves Validate
    /// below as the single place that decides which identifier is needed.
    /// </summary>
    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Enter an email address in the form name@example.com.")]
    [DataType(DataType.EmailAddress)]
    public string? Email { get; set; }

    /// <summary>
    /// Used when AccountIdentityConventions.SignInWithEmail is false. See the
    /// note on Email for why this is nullable too.
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
    /// annotations are fixed at compile time, so the rule lives here.
    ///
    /// Server-side only: unobtrusive client validation emits rules from
    /// attributes, so the browser will not pre-empt this one. The server still
    /// rejects it, which is what matters.
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
