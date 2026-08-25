using System.ComponentModel.DataAnnotations;

namespace SampleIdentityApp.Models;

/// <summary>
/// One row of a collection, rendered by
/// Views/Shared/EditorTemplates/LineItem.cshtml.
///
/// Demonstrates the collection case: `Html.EditorFor(m => m.LineItems)` renders
/// the template once per element and supplies the index, so `asp-for="Quantity"`
/// inside it becomes `name="LineItems[0].Quantity"`. Nothing in the template
/// needs to know about indexing.
/// </summary>
public class LineItem
{
    /// <summary>
    /// Kept as a hidden field so an existing row stays identifiable on the way
    /// back, which is how a controller tells an edit from an insert.
    /// </summary>
    public int Id { get; set; }

    [Display(Name = "Description")]
    [Required(ErrorMessage = "Describe this line.")]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Qty")]
    [Range(1, 9999, ErrorMessage = "Enter a quantity of at least 1.")]
    public int Quantity { get; set; } = 1;

    [Display(Name = "Unit price")]
    [Range(0, 1_000_000)]
    public decimal UnitPrice { get; set; }

    public decimal Total => Quantity * UnitPrice;
}
