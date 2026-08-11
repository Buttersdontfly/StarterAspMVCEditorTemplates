using Microsoft.AspNetCore.Identity;
using StarterAspMVCEditorTemplates.Identity;

namespace StarterAspMVCEditorTemplates.Data;

/// <summary>
/// Seeds a development user. Called from Program.cs in the Development
/// environment only, so it will not run in Staging or Production.
/// </summary>
public static class SeedData
{
    public const string DevUserEmail = "SEED-USER-EMAIL";

    /// <summary>
    /// Only used when AccountIdentityConventions.SignInWithEmail is false.
    /// </summary>
    public const string DevUserName = "devuser";

    public const string DevUserPassword = "123User!";
    public const string AdminRole = "Admin";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SeedData));

        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(AdminRole));
        }

        if (await userManager.FindByEmailAsync(DevUserEmail) is not null)
        {
            return;
        }

        // Built through the conventions rather than by hand, so the seeded user
        // follows whatever SignInWithEmail is set to without a second edit here.
        var user = AccountIdentityConventions.CreateUser(DevUserEmail, DevUserName);
        user.EmailConfirmed = true;

        var result = await userManager.CreateAsync(user, DevUserPassword);
        if (!result.Succeeded)
        {
            logger.LogError("Could not seed the development user: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, AdminRole);
        logger.LogInformation("Seeded development user {Email} with password {Password}",
            DevUserEmail, DevUserPassword);
    }
}
