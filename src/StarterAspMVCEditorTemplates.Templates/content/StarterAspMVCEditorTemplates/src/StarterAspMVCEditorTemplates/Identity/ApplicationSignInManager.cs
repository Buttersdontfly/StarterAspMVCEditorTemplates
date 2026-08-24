using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace StarterAspMVCEditorTemplates.Identity;

/// <summary>
/// Adds a disabled-account check to sign-in.
///
/// The check belongs here rather than in the controller because SignInManager is
/// consulted by every sign-in path -- password, external login, two-factor,
/// cookie refresh -- and a check in one controller action would miss the rest.
/// </summary>
public partial class ApplicationSignInManager(
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<ApplicationUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<ApplicationUser> confirmation)
    : SignInManager<ApplicationUser>(
        userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    public override async Task<bool> CanSignInAsync(ApplicationUser user)
    {
        if (!user.IsEnabled)
        {
            AttemptedLoginOfDisabledUser(Logger, user.Id);
            return false;
        }

        return await base.CanSignInAsync(user);
    }

    [LoggerMessage(EventId = 1051, Level = LogLevel.Warning,
        Message = "Attempted login by disabled user: {UserId}")]
    private static partial void AttemptedLoginOfDisabledUser(ILogger logger, Guid userId);
}
