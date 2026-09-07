using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using BeachBot.Api.Auth;

namespace BeachBot.Application.Storage;

/// <summary>Saved "remember me" data: the account email plus the Meteor resume token.</summary>
public sealed record StoredCredentials(string Email, AuthToken Token);

public interface ICredentialStore
{
    StoredCredentials? Load();
    void Save(StoredCredentials credentials);
    void Clear();
}

/// <summary>
/// File-backed credential storage for "remember me". Persists the resume token
/// (never the password) under the user's roaming app data, encrypted with the
/// Windows Data Protection API (DPAPI) scoped to the current user — so the file
/// can only be decrypted while signed in as the same Windows user on the same
/// machine. The token also carries its own expiry.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class CredentialStore : ICredentialStore
{
    private readonly string _path;
    private readonly object _gate = new();

    public CredentialStore(string? path = null)
        => _path = path ?? Path.Combine(AppPaths.DataDirectory, "credentials.dat");

    public StoredCredentials? Load()
    {
        lock (_gate)
        {
            try
            {
                if (!File.Exists(_path))
                    return null;

                var encrypted = File.ReadAllBytes(_path);
                var json = ProtectedData.Unprotect(encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);
                return JsonSerializer.Deserialize<StoredCredentials>(json, JsonFileStore.Options);
            }
            catch (Exception ex) when (ex is IOException or JsonException or CryptographicException or UnauthorizedAccessException)
            {
                // Missing, corrupt, or written by a different user/machine → treat as "not remembered".
                return null;
            }
        }
    }

    public void Save(StoredCredentials credentials)
    {
        lock (_gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var json = JsonSerializer.SerializeToUtf8Bytes(credentials, JsonFileStore.Options);
            var encrypted = ProtectedData.Protect(json, optionalEntropy: null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_path, encrypted);
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            try { File.Delete(_path); }
            catch (IOException) { /* ignore */ }
            catch (UnauthorizedAccessException) { /* ignore */ }
        }
    }
}
