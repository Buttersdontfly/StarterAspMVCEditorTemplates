using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;
using StarterAspMVCEditorTemplates.Identity;
using StarterAspMVCEditorTemplates.Models.Account;
using StarterAspMVCEditorTemplates.Services;

namespace StarterAspMVCEditorTemplates.Controllers;

/// <summary>
/// Hand-rolled account pages. Deliberately NOT the Identity.UI Razor Class
/// Library: the point of this template is that every input is rendered by an
/// editor template you own and can edit in one place.
///
/// Note that nothing here references IdentityUser.UserName. All identifier
/// decisions go through AccountIdentityConventions — see documentation/seams.md.
/// CI fails the build if that stops being true.
/// </summary>
public class AccountController(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    IAppEmailSender emailSender,
    ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginInputModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInputModel input, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var user = await AccountIdentityConventions.FindForSignInAsync(userManager, input);
        if (user is null)
        {
            // Same message whether the account exists or the password was wrong,
            // so the form cannot be used to discover which addresses are registered.
            ModelState.AddModelError(string.Empty, AccountIdentityConventions.SignInFailedMessage);
            return View(input);
        }

        var result = await signInManager.PasswordSignInAsync(
            AccountIdentityConventions.SignInIdentifier(user),
            input.Password,
            input.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            logger.LogInformation("User signed in.");
            return RedirectToLocal(returnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account is locked. Try again later.");
            return View(input);
        }

        if (result.IsNotAllowed)
        {
            ModelState.AddModelError(string.Empty, "Confirm your email address before signing in.");
            return View(input);
        }

        ModelState.AddModelError(string.Empty, AccountIdentityConventions.SignInFailedMessage);
        return View(input);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterInputModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterInputModel input, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var user = AccountIdentityConventions.CreateUser(input);
        var result = await userManager.CreateAsync(user, input.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(input);
        }

        logger.LogInformation("New account created.");

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = Url.Action(nameof(ConfirmEmail), "Account",
            new { userId = user.Id, token = encoded }, Request.Scheme)!;

        await emailSender.SendAsync(input.Email, "Confirm your email",
            $"""
             <p>Confirm your email address to finish setting up your account.</p>
             <p><a href="{HtmlEncoder.Default.Encode(link)}">Confirm email</a></p>
             """);

        if (userManager.Options.SignIn.RequireConfirmedAccount)
        {
            return RedirectToAction(nameof(RegisterConfirmation));
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToLocal(returnUrl);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult RegisterConfirmation() => View();

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return View("ConfirmEmailFailed");
        }

        var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        var result = await userManager.ConfirmEmailAsync(user, decoded);

        return View(result.Succeeded ? "ConfirmEmail" : "ConfirmEmailFailed");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordInputModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordInputModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var user = await AccountIdentityConventions.FindForPasswordResetAsync(userManager, input.Email);

        // Always report the same outcome, so the form cannot be used to find out
        // which addresses have accounts.
        //
        // Written as a nested if rather than a compound condition: the compiler
        // cannot prove `user` is non-null across `a && b || a && c`, and with
        // TreatWarningsAsErrors that is a build failure, not a warning.
        if (user is not null)
        {
            var mayReset = !userManager.Options.SignIn.RequireConfirmedAccount
                           || await userManager.IsEmailConfirmedAsync(user);

            if (mayReset)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var link = Url.Action(nameof(ResetPassword), "Account",
                    new { email = input.Email, token = encoded }, Request.Scheme)!;

                await emailSender.SendAsync(input.Email, "Reset your password",
                    $"""
                     <p>Use this link to choose a new password.</p>
                     <p><a href="{HtmlEncoder.Default.Encode(link)}">Reset password</a></p>
                     <p>If you did not ask for this, no action is needed.</p>
                     """);
            }
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return View("ResetPasswordFailed");
        }

        return View(new ResetPasswordInputModel { Email = email ?? string.Empty, Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordInputModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var user = await AccountIdentityConventions.FindForPasswordResetAsync(userManager, input.Email);
        if (user is null)
        {
            // Do not reveal that the account does not exist.
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(input.Token));
        var result = await userManager.ResetPasswordAsync(user, decoded, input.Password);

        if (result.Succeeded)
        {
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(input);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation() => View();

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordInputModel());

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordInputModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(input);
        }

        await signInManager.RefreshSignInAsync(user);
        TempData["StatusMessage"] = "Your password has been changed.";
        return RedirectToAction(nameof(ChangePassword));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToLocal(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
}
