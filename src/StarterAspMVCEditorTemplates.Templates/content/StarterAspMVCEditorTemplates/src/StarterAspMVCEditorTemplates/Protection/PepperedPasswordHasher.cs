using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace StarterAspMVCEditorTemplates.Protection;

/// <summary>
/// Wraps Identity's PasswordHasher and mixes a secret pepper into the password
/// before hashing.
///
/// The base hasher already salts and stretches with PBKDF2, so this adds exactly
/// one property: a stolen database is not enough on its own, because the attacker
/// also needs the pepper from the application's configuration.
///
/// Concatenation is safe here because the base hasher is PBKDF2, which accepts
/// arbitrary-length input. It would NOT be safe with bcrypt, which truncates at
/// 72 bytes -- a long password plus a pepper could push the pepper out of range
/// entirely. Worth remembering if you ever swap the base hasher.
/// </summary>
public sealed class PepperedPasswordHasher<TUser> : PasswordHasher<TUser>
    where TUser : class
{
    private readonly string _pepper;

    public PepperedPasswordHasher(
        IOptions<PasswordHasherOptions> options,
        IOptions<PepperOptions> pepperOptions)
        : base(options)
    {
        _pepper = pepperOptions.Value.Value;
    }

    public override string HashPassword(TUser user, string password) =>
        base.HashPassword(user, Combine(password));

    public override PasswordVerificationResult VerifyHashedPassword(
        TUser user, string hashedPassword, string providedPassword) =>
        base.VerifyHashedPassword(user, hashedPassword, Combine(providedPassword));

    private string Combine(string password) => password + _pepper;
}
