using Microsoft.AspNetCore.Identity;

namespace StarterAspMVCEditorTemplates.Identity;

/// <summary>
/// The application's role. Empty for the same reason as ApplicationUser: it is
/// cheaper to have it and not need it than to introduce it later, since doing so
/// changes the schema.
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }

    public ApplicationRole(string roleName) : base(roleName) { }
}
