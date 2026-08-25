using Microsoft.AspNetCore.Identity;
using SampleIdentityApp.Exceptions;
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
public partial class DevelopmentDataSeeder : IDataInitializer
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DevelopmentDataSeeder> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public DevelopmentDataSeeder(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, ILoggerFactory loggerFactory)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<DevelopmentDataSeeder>();
    }

    public const string DevUserEmail = "dev@localhost";
    public const string DevUserName = "devuser";
    public const string DevUserPassword = "123User!";

    public async Task SeedDataAsync()
    {
        var roleSeed = new IdentityRoleSeeder(_roleManager, _loggerFactory.CreateLogger<IdentityRoleSeeder>());
        await roleSeed.EnsuresRoleCreatedAsync();
        await EnsureDevUserAsync();
    }

    private async Task EnsureDevUserAsync()
    {
        if (await _userManager.FindByEmailAsync(DevUserEmail) is not null)
        {
            return;
        }

        var user = AccountIdentityConventions.CreateUser(DevUserEmail, DevUserName);
        user.EmailConfirmed = true;

        IdentityResult result;
        try
        {
            result = await _userManager.CreateAsync(user, DevUserPassword);
        }
        catch (Exception ex) when (ex.IsUniqueConstraintViolation())
        {
            Log.ConcurrentDevUserCreation(_logger);
            return;
        }

        if (!result.Succeeded)
        {
            if (await _userManager.FindByEmailAsync(DevUserEmail) is not null)
            {
                return;
            }

            Log.FailedDevUserCreation(_logger, string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        try
        {
            await _userManager.AddToRoleAsync(user, Roles.Admin);
        }
        catch (Exception ex) when (ex.IsUniqueConstraintViolation())
        {
            Log.DevUserAlreadyInRole(_logger, Roles.Admin);
        }

        Log.CreatedDevUser(_logger, DevUserEmail, DevUserPassword);
    }


}
