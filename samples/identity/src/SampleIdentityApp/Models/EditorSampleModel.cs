using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SampleIdentityApp.Models;

/// <summary>
/// Backs the /dev/editors gallery. Every editor template in the project must be
/// reachable from here.
///
/// This is not decoration: EditorTemplateTests asserts that each expected field
/// renders, so adding a template without adding a property here fails the build.
/// That is what stops editor-template coverage rotting as the project grows.
///
/// Each property is also a worked example of how a template gets chosen -- read
/// the attributes alongside documentation/editor-templates.md.
/// </summary>
public class EditorSampleModel
{
    // --- Resolved by type name -------------------------------------------

    [Display(Name = "Plain text", Prompt = "Anything at all",
        Description = "Any string with no [UIHint] and no [DataType] lands on String.cshtml.")]
    [Required]
    public string PlainText { get; set; } = string.Empty;

    [Display(Name = "Whole number", Description = "[Range] bounds become min and max on the input.")]
    [Range(0, 100)]
    public int WholeNumber { get; set; }

    [Display(Name = "Amount", Description = "Currency symbol is overridable per field.")]
    public decimal Amount { get; set; }

    [Display(Name = "Enabled", Description = "Rendered as a switch rather than a bare checkbox.")]
    public bool Enabled { get; set; }

    [Display(Name = "Date")]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(System.DateTime.Today);

    [Display(Name = "Time")]
    public TimeOnly Time { get; set; } = new(9, 0);

    [Display(Name = "Timestamp")]
    public System.DateTime Timestamp { get; set; } = System.DateTime.Now;

    [Display(Name = "Status", Description = "Enums resolve to Enum.cshtml automatically.")]
    public SampleStatus Status { get; set; }

    [Display(Name = "Optional status", Description = "A nullable enum gains an empty option.")]
    public SampleStatus? OptionalStatus { get; set; }

    // --- Resolved by [DataType] ------------------------------------------

    [Display(Name = "Email")]
    [DataType(DataType.EmailAddress)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Password")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Website", Prompt = "example.com")]
    [DataType(DataType.Url)]
    public string Website { get; set; } = string.Empty;

    [Display(Name = "Phone", Prompt = "+43 1 234 5678")]
    [DataType(DataType.PhoneNumber)]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Notes", Description = "Row count is overridable per field.")]
    [DataType(DataType.MultilineText)]
    public string Notes { get; set; } = string.Empty;

    // --- Forced with [UIHint] --------------------------------------------

    [Display(Name = "Country", Description = "Options come from the view, not the template.")]
    [UIHint("Dropdown")]
    public string CountryCode { get; set; } = string.Empty;

    [Display(Name = "Preferred contact")]
    [UIHint("RadioGroup")]
    public SampleStatus PreferredContact { get; set; }

    [Display(Name = "Interests", Description = "Several checkboxes sharing one name post as a list.")]
    [UIHint("CheckboxList")]
    public List<int> Interests { get; set; } = [];

    [Display(Name = "Tags", Description = "Hidden inputs, one per tag, collected into a list.")]
    [UIHint("Tags")]
    public List<string> Tags { get; set; } = [];

    [Display(Name = "Brand colour")]
    [UIHint("Color")]
    public string BrandColor { get; set; } = "#0d6efd";

    [Display(Name = "Satisfaction", Description = "Radio buttons styled as stars. No JavaScript.")]
    [UIHint("Rating")]
    [Range(1, 5)]
    public int Satisfaction { get; set; } = 3;

    [Display(Name = "Volume", Description = "Bounds are read from [Range] on this property.")]
    [UIHint("Range")]
    [Range(0, 11)]
    public int Volume { get; set; } = 7;

    [Display(Name = "Attachment")]
    [UIHint("FileUpload")]
    public IFormFile? Attachment { get; set; }

    [Display(Name = "Username")]
    [UIHint("UserName")]
    public string? UserName { get; set; }

    // --- Complex types, resolved by type name ----------------------------

    [Display(Name = "Name")]
    public PersonNameInputModel Name { get; set; } = new();

    [Display(Name = "Address")]
    public AddressInputModel Address { get; set; } = new();

    [Display(Name = "Line items", Description = "One template per element, indexes supplied by MVC.")]
    public List<LineItem> LineItems { get; set; } = [];

    /// <summary>Options for the Dropdown sample.</summary>
    public static IEnumerable<SelectListItem> CountryItems =>
        Countries.All.Select(c => new SelectListItem(c.Name, c.Code));

    /// <summary>Options for the CheckboxList sample.</summary>
    public static IEnumerable<SelectListItem> InterestItems =>
    [
        new("Architecture", "1"),
        new("Testing", "2"),
        new("Tooling", "3"),
        new("Accessibility", "4"),
        new("Performance", "5"),
        new("Security", "6")
    ];
}

public enum SampleStatus
{
    [Display(Name = "Draft")] Draft,
    [Display(Name = "In review")] InReview,
    [Display(Name = "Published")] Published
}
