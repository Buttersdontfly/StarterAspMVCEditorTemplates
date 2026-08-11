using System.ComponentModel.DataAnnotations;

namespace StarterAspMVCEditorTemplates.Models;

/// <summary>
/// Rendered by Views/Shared/EditorTemplates/AddressInputModel.cshtml.
/// Resolves by TYPE NAME — see documentation/editor-templates.md.
/// </summary>
public class AddressInputModel
{
    [Display(Name = "Street address")]
    [Required(ErrorMessage = "Enter a street address.")]
    [StringLength(200)]
    public string Line1 { get; set; } = string.Empty;

    [Display(Name = "Apartment, suite, etc. (optional)")]
    [StringLength(200)]
    public string? Line2 { get; set; }

    [Display(Name = "City")]
    [Required(ErrorMessage = "Enter a city.")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Display(Name = "State or region")]
    [StringLength(100)]
    public string? Region { get; set; }

    [Display(Name = "Postal code")]
    [Required(ErrorMessage = "Enter a postal code.")]
    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [Display(Name = "Country")]
    [Required(ErrorMessage = "Select a country.")]
    public string Country { get; set; } = "AT";
}

/// <summary>
/// Backs the country select. A record rather than a tuple on purpose:
/// SelectList reflects over PROPERTIES, and ValueTuple exposes fields, so
/// tuples fail at runtime. Replace with your own source if you need the full
/// ISO list or a localised one.
/// </summary>
public sealed record Country(string Code, string Name);

public static class Countries
{
    public static readonly IReadOnlyList<Country> All =
    [
        new("AT", "Austria"),
        new("DE", "Germany"),
        new("CH", "Switzerland"),
        new("FR", "France"),
        new("IT", "Italy"),
        new("GB", "United Kingdom"),
        new("US", "United States")
    ];
}
