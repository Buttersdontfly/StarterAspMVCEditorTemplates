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

    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SeedData));

        await EnsureRoleAsync(roleManager, Roles.Admin, logger);
        await EnsureDevUserAsync(userManager, logger);
    }

    private static async Task EnsureRoleAsync(
        RoleManager<ApplicationRole> roleManager, string role, ILogger logger)
    {
        if (await roleManager.RoleExistsAsync(role))
        {
            return;
        }

        try
        {
            var result = await roleManager.CreateAsync(new ApplicationRole(role));
            if (!result.Succeeded && !await roleManager.RoleExistsAsync(role))
            {
                logger.LogError("Could not create the {Role} role: {Errors}",
                    role, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
        catch (Exception ex) when (IsUniqueConstraintViolation(ex))
        {
            // Another process created it between the check and the insert.
            logger.LogDebug("The {Role} role was created concurrently.", role);
        }
    }

    private static async Task EnsureDevUserAsync(
        UserManager<ApplicationUser> userManager, ILogger logger)
    {
        if (await userManager.FindByEmailAsync(DevUserEmail) is not null)
        {
            return;
        }

        // Built through the conventions rather than by hand, so the seeded user
        // follows whatever SignInWithEmail is set to without a second edit here.
        var user = AccountIdentityConventions.CreateUser(DevUserEmail, DevUserName);
        user.EmailConfirmed = true;

        IdentityResult result;
        try
        {
            result = await userManager.CreateAsync(user, DevUserPassword);
        }
        catch (Exception ex) when (IsUniqueConstraintViolation(ex))
        {
            logger.LogDebug("The development user was created concurrently.");
            return;
        }

        if (!result.Succeeded)
        {
            // A duplicate here means another process won the race, which is fine.
            if (await userManager.FindByEmailAsync(DevUserEmail) is not null)
            {
                return;
            }

            logger.LogError("Could not seed the development user: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        try
        {
            await userManager.AddToRoleAsync(user, Roles.Admin);
        }
        catch (Exception ex) when (IsUniqueConstraintViolation(ex))
        {
            logger.LogDebug("The development user was already in the {Role} role.", Roles.Admin);
        }

        logger.LogInformation("Seeded development user {Email} with password {Password}",
            DevUserEmail, DevUserPassword);
    }

    /// <summary>
    /// Matched on the message rather than on a provider-specific exception type,
    /// so this keeps working after the database provider is swapped. The text
    /// differs per provider, hence the several variants.
    /// </summary>
    private static bool IsUniqueConstraintViolation(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var message = current.Message;

            if (message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
                || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Violation of UNIQUE KEY", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
