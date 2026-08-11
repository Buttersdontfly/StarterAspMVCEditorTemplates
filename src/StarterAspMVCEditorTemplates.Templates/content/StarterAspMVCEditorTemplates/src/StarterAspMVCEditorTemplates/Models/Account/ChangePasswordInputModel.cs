using System.ComponentModel.DataAnnotations;

namespace StarterAspMVCEditorTemplates.Models.Account;

public class ChangePasswordInputModel
{
    [Display(Name = "Current password")]
    [Required(ErrorMessage = "Enter your current password.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Display(Name = "New password")]
    [Required(ErrorMessage = "Choose a new password.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Display(Name = "Confirm new password")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "The two passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
