using Microsoft.AspNetCore.Identity;
using SampleIdentityApp.Identity;

namespace SampleIdentityApp.Services;

/// <summary>
/// Adapts the app's IAppEmailSender onto the interface Identity expects, so the
/// rest of the app never depends on Identity's email abstraction.
/// </summary>
public sealed class IdentityEmailSenderAdapter(IAppEmailSender sender) : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        sender.SendAsync(email, "Confirm your email",
            $"""
             <p>Confirm your email address to finish setting up your account.</p>
             <p><a href="{confirmationLink}">Confirm email</a></p>
             """);

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        sender.SendAsync(email, "Reset your password",
            $"""
             <p>Use this link to choose a new password. It expires shortly.</p>
             <p><a href="{resetLink}">Reset password</a></p>
             <p>If you did not ask for this, no action is needed.</p>
             """);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        sender.SendAsync(email, "Your password reset code",
            $"<p>Your password reset code is <strong>{resetCode}</strong>.</p>");
}
