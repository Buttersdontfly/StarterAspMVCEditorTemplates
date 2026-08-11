using System.ComponentModel.DataAnnotations;

namespace StarterAspMVCEditorTemplates.Models;

/// <summary>
/// Rendered by Views/Shared/EditorTemplates/PersonNameInputModel.cshtml.
/// Complex types resolve their editor template by TYPE NAME, so the file name
/// must match this class name. See documentation/editor-templates.md.
/// </summary>
public class PersonNameInputModel
{
    [Display(Name = "First name")]
    [Required(ErrorMessage = "Enter a first name.")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Last name")]
    [Required(ErrorMessage = "Enter a last name.")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    public override string ToString() => $"{FirstName} {LastName}".Trim();
}
