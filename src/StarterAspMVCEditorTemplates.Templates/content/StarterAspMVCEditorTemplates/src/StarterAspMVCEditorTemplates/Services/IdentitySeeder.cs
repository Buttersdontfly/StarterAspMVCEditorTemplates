using Microsoft.AspNetCore.Identity;
using StarterAspMVCEditorTemplates.Identity;

namespace StarterAspMVCEditorTemplates.Services;

internal partial class IdentitySeeder
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, ILogger<IdentitySeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
    }

    private async Task SeedRolesAsync()
    {
        var roles = ReflectionHelper.GetAllRoles();

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await SeedRoleAsync(role);
            }
        }
    }

    private async Task SeedRoleAsync(string role)
    {
        IdentityResult result = await _roleManager.CreateAsync(new ApplicationRole { Name = role });

        if (!result.Succeeded)
        {
            Log.FailedRoleCreation(_logger, role);
        }

        Log.CreatedNewRole(_logger, role);
    }
}
