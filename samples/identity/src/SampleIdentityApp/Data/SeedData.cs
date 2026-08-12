using Microsoft.AspNetCore.Identity;
using SampleIdentityApp.Identity;

namespace SampleIdentityApp.Data;

/// <summary>
/// Seeds a development user. Called from Program.cs in the Development
/// environment only, so it will not run in Staging or Production.
/// </summary>
/// <remarks>
/// Written to tolerate running concurrently against the same database.
///
/// "Check whether it exists, then create it" is a race: two processes can both
/// see it missing and both insert, and the loser gets
/// `UNIQUE constraint failed: AspNetRoles.NormalizedName`. That is not
/// hypothetical -- xUnit runs test classes in parallel, so several
/// WebApplicationFactory instances start at once, and a developer running the
/// app while its tests run hits the same thing.
///
/// So each step treats "someone else created it first" as success rather than as
/// an error. Losing the race is a valid outcome; the goal is that the row exists
/// afterwards, not that this caller is the one who inserted it.
/// </remarks>
public static class SeedData
{
    public const string DevUserEmail = "dev@localhost";

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

        await EnsureRoleAsync(roleManager, AdminRole, logger);
        await EnsureDevUserAsync(userManager, logger);
    }

    private static async Task EnsureRoleAsync(
        RoleManager<IdentityRole> roleManager, string role, ILogger logger)
    {
        if (await roleManager.RoleExistsAsync(role))
        {
            return;
        }

        try
        {
            var result = await roleManager.CreateAsync(new IdentityRole(role));
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
        UserManager<IdentityUser> userManager, ILogger logger)
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
            await userManager.AddToRoleAsync(user, AdminRole);
        }
        catch (Exception ex) when (IsUniqueConstraintViolation(ex))
        {
            logger.LogDebug("The development user was already in the {Role} role.", AdminRole);
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
