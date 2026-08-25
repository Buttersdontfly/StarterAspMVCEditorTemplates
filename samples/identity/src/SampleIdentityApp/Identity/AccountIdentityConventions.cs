using Microsoft.AspNetCore.Identity;
using SampleIdentityApp.Models.Account;

namespace SampleIdentityApp.Identity;

/// <summary>
/// SEAM: username identity.
///
/// The ONLY file that decides how a login identifier maps onto
/// ApplicationUser.UserName. Nothing else in the project touches UserName --
/// not the controllers, not the views, not the seeder. That invariant is
/// enforced by a test (GenerationTests.UserName_is_only_touched_by_the_conventions_class)
/// and by build/Lint-Generated.ps1, so the coupling cannot spread without
/// failing the build.
///
/// To separate username from email, flip <see cref="SignInWithEmail"/> to false.
/// That single change is enough: the conventions below branch on it, the login
/// and register views show the right fields, and validation follows. Nothing
/// needs uncommenting.
/// </summary>
public static class AccountIdentityConventions
{
    /// <summary>
    /// true  -- the user signs in with their email, and UserName mirrors it.
    ///          Register asks for email and password only.
    /// false -- the user picks a separate username and signs in with that.
    ///          Register asks for username as well; login asks for username
    ///          instead of email.
    ///
    /// Deliberately `static readonly` and NOT `const`.
    ///
    /// A `const bool` is folded at compile time, so every branch for the other
    /// mode becomes unreachable and the compiler raises CS0162. With
    /// TreatWarningsAsErrors on -- which this template sets -- that is a build
    /// failure, in the generated Razor views as well as here. The seam would
    /// then only compile in one of the two modes, which defeats the point.
    ///
    /// `static readonly` keeps the flip to a single edit and costs one field
    /// read per branch, which is not worth optimising.
    /// </summary>
    public static readonly bool SignInWithEmail = true;

    /// <summary>
    /// Builds a new user. <paramref name="userName"/> is ignored when
    /// <see cref="SignInWithEmail"/> is true, and required when it is false.
    /// </summary>
    public static ApplicationUser CreateUser(string email, string? userName = null)
    {
        if (SignInWithEmail)
        {
            return new ApplicationUser { UserName = email, Email = email };
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException(
                "A username is required when SignInWithEmail is false.", nameof(userName));
        }

        return new ApplicationUser { UserName = userName, Email = email };
    }

    public static ApplicationUser CreateUser(RegisterInputModel input) =>
        CreateUser(input.Email, input.UserName);

    public static async Task<ApplicationUser?> FindForSignInAsync(
        UserManager<ApplicationUser> userManager, LoginInputModel input) =>
        SignInWithEmail
            ? await userManager.FindByEmailAsync(input.Email ?? string.Empty)
            : await userManager.FindByNameAsync(input.UserName ?? string.Empty);

    /// <summary>
    /// Password reset is always keyed on email: the reset link is delivered
    /// there, so a username would add nothing.
    /// </summary>
    public static async Task<ApplicationUser?> FindForPasswordResetAsync(
        UserManager<ApplicationUser> userManager, string email) =>
        await userManager.FindByEmailAsync(email);

    /// <summary>
    /// The value handed to SignInManager, which always works in terms of UserName.
    /// </summary>
    public static string SignInIdentifier(ApplicationUser user) =>
        user.UserName ?? user.Email ?? string.Empty;

    /// <summary>
    /// What to call the login field in messages, so copy stays consistent when
    /// the two are separated.
    /// </summary>
    public static string IdentifierDisplayName => SignInWithEmail ? "email" : "username";

    /// <summary>
    /// Whether the sign-in failure message should mention email or username.
    /// </summary>
    public static string SignInFailedMessage =>
        SignInWithEmail
            ? "That email and password do not match an account."
            : "That username and password do not match an account.";
}
