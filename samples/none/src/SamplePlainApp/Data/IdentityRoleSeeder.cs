using Microsoft.AspNetCore.Identity;
using SamplePlainApp.Exceptions;
using SamplePlainApp.Identity;
using SamplePlainApp.Utilities;

namespace SamplePlainApp.Data;

internal partial class IdentityRoleSeeder
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<IdentityRoleSeeder> _logger;

    public IdentityRoleSeeder(RoleManager<ApplicationRole> roleManager, ILogger<IdentityRoleSeeder> logger)
    {
        _roleManager = roleManager;
        _logger = logger;
    }
    public async Task EnsuresRoleCreatedAsync()
    {
        IEnumerable<string> roles = ReflectionHelper.GetAllRoles();

        foreach (var role in roles)
        {
            await CreateRoleAsync(role);
        }
    }

    private async Task CreateRoleAsync(string role)
    {
        if (await _roleManager.RoleExistsAsync(role))
        {
            return;
        }

        try
        {
            IdentityResult result = await _roleManager.CreateAsync(new ApplicationRole { Name = role });

            if (!result.Succeeded && !await _roleManager.RoleExistsAsync(role))
            {
                Log.FailedRoleCreation(_logger, role, string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }
        }
        catch (Exception ex) when (ex.IsUniqueConstraintViolation())
        {
            Log.UniqueKeyViolation(_logger, role);
        }

        Log.CreatedNewRole(_logger, role);
    }
}

