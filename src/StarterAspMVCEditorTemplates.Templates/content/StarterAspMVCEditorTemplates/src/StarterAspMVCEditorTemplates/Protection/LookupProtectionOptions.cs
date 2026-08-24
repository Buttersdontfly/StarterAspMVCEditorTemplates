using System.ComponentModel.DataAnnotations;

namespace StarterAspMVCEditorTemplates.Protection;

/// <summary>
/// Keys for the lookup protector, which encrypts personal data columns.
///
/// LOSING THESE MAKES THE PROTECTED DATA UNRECOVERABLE. There is no reset flow:
/// the email addresses in the database are ciphertext, so without the key the
/// accounts cannot be found, matched or restored. Back the keys up somewhere
/// other than the database they protect.
///
/// Keys are held by id so they can be rotated. Add a new id, point
/// <see cref="CurrentKeyId"/> at it, and keep the old entry: rows written under
/// the old key remain readable, because Identity stores the key id alongside the
/// value.
/// </summary>
public sealed class LookupProtectionOptions
{
    public const string SectionName = "LookupProtection";

    /// <summary>The key id new values are written under.</summary>
    [Required(AllowEmptyStrings = false)]
    public string CurrentKeyId { get; set; } = "v1";

    /// <summary>Key id to key material. Keep old entries after rotating.</summary>
    [MinLength(1, ErrorMessage = "At least one lookup protection key is required.")]
    public Dictionary<string, string> Keys { get; set; } = [];
}
