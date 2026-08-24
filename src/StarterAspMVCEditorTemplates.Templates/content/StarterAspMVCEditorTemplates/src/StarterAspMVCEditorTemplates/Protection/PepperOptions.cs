using System.ComponentModel.DataAnnotations;

namespace StarterAspMVCEditorTemplates.Protection;

/// <summary>
/// The pepper: a secret mixed into every password before hashing.
///
/// A salt defends against precomputation and is stored beside the hash. A pepper
/// defends against an attacker who has the database but NOT the application
/// configuration, so it is only worth anything while it lives somewhere the
/// database dump does not.
///
/// Two consequences worth knowing before you change it:
///
///   1. Changing the pepper invalidates every existing password hash. Nobody can
///      sign in afterwards, and there is no migration: the old passwords cannot
///      be re-hashed without the plaintext. Treat it as permanent, or plan a
///      forced reset.
///   2. A pepper in source control is not a secret. The value generated into
///      appsettings.Development.json is for local development only.
/// </summary>
public sealed class PepperOptions
{
    public const string SectionName = "Pepper";

    [Required(AllowEmptyStrings = false)]
    [MinLength(16, ErrorMessage = "A pepper shorter than 16 characters is not worth having.")]
    public string Value { get; set; } = string.Empty;
}
