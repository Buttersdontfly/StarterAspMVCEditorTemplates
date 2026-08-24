using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace StarterAspMVCEditorTemplates.Protection;

/// <summary>
/// Supplies the keys used by <see cref="LookupProtector"/>.
///
/// Reads from configuration rather than holding a constant, so the key can differ
/// per environment and never has to be committed. See LookupProtectionOptions for
/// the rotation story and the warning about losing keys.
/// </summary>
public sealed class KeyRing(IOptions<LookupProtectionOptions> options) : ILookupProtectorKeyRing
{
    private readonly LookupProtectionOptions _options = options.Value;

    public string this[string keyId] =>
        _options.Keys.TryGetValue(keyId, out var key)
            ? key
            : throw new KeyNotFoundException(
                $"No lookup protection key with id '{keyId}'. It is configured under " +
                $"'{LookupProtectionOptions.SectionName}:Keys'. If this key existed once and has " +
                "been removed, any data written under it is now unreadable -- restore it from backup.");

    public string CurrentKeyId => _options.CurrentKeyId;

    public IEnumerable<string> GetAllKeyIds() => _options.Keys.Keys;
}
