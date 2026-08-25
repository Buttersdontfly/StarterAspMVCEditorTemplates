using Microsoft.AspNetCore.Identity;
using StarterAspMVCEditorTemplates.Identity;

namespace StarterAspMVCEditorTemplates.Data;

public partial class ProductionDataSeeder : IDataInitializer
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ProductionDataSeeder> _logger;
    private static IEnumerable<ProdUser> SeededUsers()
    {
        throw new NotImplementedException();

        //yield return new ProdUser
        //{

        //};
    }

    public ProductionDataSeeder(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, ILoggerFactory loggerFactory)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<ProductionDataSeeder>();
    }

    public async Task SeedDataAsync()
    {
        var roleSeed = new IdentityRoleSeeder(_roleManager, _loggerFactory.CreateLogger<IdentityRoleSeeder>());
        await roleSeed.EnsuresRoleCreatedAsync();
        await SeedUsersAsync();
    }

    private async Task SeedUsersAsync()
    {
        foreach (var e in SeededUsers())
        {
            if (await _userManager.FindByEmailAsync(e.User.Email!) is null)
            {
                await SeedUserAsync(e);
            }
        }
    }
    private async Task SeedUserAsync(ProdUser e)
    {
        IdentityResult result = await _userManager.CreateAsync(e.User);

        if (!result.Succeeded)
        {
            Log.FailedUserCreation(_logger, e.User.Email ?? "No Email");
            return;
        }

        result = await _userManager.AddToRolesAsync(e.User, e.Roles);

        if (_logger.IsEnabled(LogLevel.Information) && result.Succeeded)
        {
            var roles = string.Join(", ", e.Roles);
            Log.CreatedNewUser(_logger, e.User.Email!, roles);
        }
    }

    private record ProdUser(ApplicationUser User, IEnumerable<string> Roles);
}
