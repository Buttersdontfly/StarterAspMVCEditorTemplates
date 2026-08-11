using System.ComponentModel.DataAnnotations;

namespace StarterAspMVCEditorTemplates.Models.Account;

public class ForgotPasswordInputModel
{
    [Display(Name = "Email")]
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter an email address in the form name@example.com.")]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = string.Empty;
}
