using Microsoft.AspNetCore.Identity;

namespace SampleIdentityApp.Identity;

/// <summary>
/// The application's user.
///
/// Kept as close to empty as it usefully can be: it exists so that adding a
/// property later is an edit rather than a schema migration away from the
/// framework type.
///
/// Keyed on Guid rather than the default string. A Guid key is opaque, leaks
/// neither row counts nor creation order, and stays stable if rows move between
/// databases. The cost is index width, which is irrelevant at this scale.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Whether the account may sign in. Checked by ApplicationSignInManager.
    ///
    /// This is the one property that is not "empty", and it earns its place:
    /// disabling an account is otherwise done by deleting it or by mangling the
    /// password hash, both of which lose information. Delete this property and
    /// the override in ApplicationSignInManager together if you do not want it.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    // Add your own properties here, then:
    //     dotnet ef migrations add AddUserProperties
    //
    // With --auth protected, mark anything personal so it is encrypted at rest
    // and exported or deleted correctly by Identity's data tooling:
    //
    // [ProtectedPersonalData]
    // public string? DisplayName { get; set; }
}
