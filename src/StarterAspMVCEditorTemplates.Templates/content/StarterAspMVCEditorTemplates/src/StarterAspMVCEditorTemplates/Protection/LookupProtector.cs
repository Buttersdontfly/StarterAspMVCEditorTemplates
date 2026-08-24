using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace StarterAspMVCEditorTemplates.Protection;

/// <summary>
/// Encrypts personal data columns so they can still be looked up.
/// </summary>
/// <remarks>
/// The encryption is DELIBERATELY DETERMINISTIC: the IV is derived from an HMAC
/// of the plaintext, so the same input always produces the same ciphertext. That
/// is what makes the column searchable -- Identity finds a user by encrypting the
/// address it was given and comparing. Random IVs would be stronger and would
/// also make FindByEmailAsync impossible.
///
/// Accept the consequence knowingly: equal plaintexts are visibly equal in the
/// database. An attacker with a database dump cannot read an address, but can
/// tell that two rows share one, and can confirm a guess by encrypting it. That
/// trade is inherent to searchable encryption, not to this implementation.
///
/// Derived keys are cached per key id. Deriving costs 100,000 PBKDF2 iterations,
/// and this runs on every lookup, sign-in and registration -- deriving each time
/// added tens of milliseconds to every one of them.
/// </remarks>
#pragma warning disable CA5401 // A non-default IV is the point; see above.
public sealed class LookupProtector(ILookupProtectorKeyRing keyRing) : ILookupProtector
{
    private const int IvSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    private readonly ConcurrentDictionary<string, (byte[] Encryption, byte[] Mac)> _derivedKeys = new();

    public string Protect(string keyId, string? data)
    {
        if (data is null) { return null!; }

        var (encryptionKey, macKey) = DeriveKeys(keyId);
        var plaintext = Encoding.UTF8.GetBytes(data);

        // IV from an HMAC of the plaintext: deterministic, and still different
        // for different inputs, which is what CBC needs to avoid leaking block
        // structure across rows.
        var iv = new byte[IvSize];
        using (var hmac = new HMACSHA256(macKey))
        {
            var tag = hmac.ComputeHash(plaintext);
            Buffer.BlockCopy(tag, 0, iv, 0, IvSize);
        }

        using var aes = Aes.Create();
        aes.Key = encryptionKey;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        var cipher = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        var result = new byte[IvSize + cipher.Length];
        Buffer.BlockCopy(iv, 0, result, 0, IvSize);
        Buffer.BlockCopy(cipher, 0, result, IvSize, cipher.Length);

        return Convert.ToBase64String(result);
    }

    public string Unprotect(string keyId, string? data)
    {
        if (data is null) { return null!; }

        var (encryptionKey, macKey) = DeriveKeys(keyId);
        var full = Convert.FromBase64String(data);

        if (full.Length < IvSize || (full.Length - IvSize) % 16 != 0)
        {
            throw new CryptographicException("Protected value is malformed.");
        }

        var iv = new byte[IvSize];
        Buffer.BlockCopy(full, 0, iv, 0, IvSize);

        var cipher = new byte[full.Length - IvSize];
        Buffer.BlockCopy(full, IvSize, cipher, 0, cipher.Length);

        using var aes = Aes.Create();
        aes.Key = encryptionKey;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        // Recompute the IV from the recovered plaintext and require it to match.
        // Because the IV is an HMAC of the plaintext, this doubles as the
        // integrity check that AES-CBC does not provide on its own: without it,
        // tampered ciphertext decrypts to garbage that the application would then
        // treat as a real value.
        using (var hmac = new HMACSHA256(macKey))
        {
            var expected = hmac.ComputeHash(plain);
            if (!CryptographicOperations.FixedTimeEquals(
                    expected.AsSpan(0, IvSize), iv.AsSpan()))
            {
                throw new CryptographicException(
                    "Protected value failed its integrity check. It was written under a different key, or it has been altered.");
            }
        }

        return Encoding.UTF8.GetString(plain);
    }

    private (byte[] Encryption, byte[] Mac) DeriveKeys(string keyId) =>
        _derivedKeys.GetOrAdd(keyId, id =>
        {
            var master = Encoding.UTF8.GetBytes(keyRing[id]);
            var salt = Encoding.UTF8.GetBytes(id);

            var derived = Rfc2898DeriveBytes.Pbkdf2(
                master, salt, Iterations, HashAlgorithmName.SHA256, KeySize * 2);

            var encryptionKey = new byte[KeySize];
            var macKey = new byte[KeySize];
            Buffer.BlockCopy(derived, 0, encryptionKey, 0, KeySize);
            Buffer.BlockCopy(derived, KeySize, macKey, 0, KeySize);
            return (encryptionKey, macKey);
        });
}
#pragma warning restore CA5401
