using Microsoft.AspNetCore.Identity;
using StarterAspMVCEditorTemplates.Identity;
using StarterAspMVCEditorTemplates.Utilities;

namespace StarterAspMVCEditorTemplates.Services;

public partial class IdentitySeeder
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

    private static partial class Log
    {
        [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Created new role: {Role}")]
        public static partial void CreatedNewRole(ILogger logger, string role);

        [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "Failed to create role: {Role}")]
        public static partial void FailedRoleCreation(ILogger logger, string role);

        [LoggerMessage(EventId = 1012, Level = LogLevel.Information, Message = "Created new user: {User}")]
        public static partial void CreatedNewUser(ILogger logger, string user);

        [LoggerMessage(EventId = 1013, Level = LogLevel.Error, Message = "Failed to create user: {User}")]
        public static partial void FailedUserCreation(ILogger logger, string user);
    }
}
