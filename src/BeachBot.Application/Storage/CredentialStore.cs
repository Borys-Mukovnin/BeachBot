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
/// File-backed credential storage for "remember me". Stores the resume token
/// (not the password) under the user's roaming app data; the token expires and is
/// only readable by the current Windows user profile.
/// </summary>
public sealed class CredentialStore : ICredentialStore
{
    private readonly string _path;
    private readonly object _gate = new();

    public CredentialStore(string? path = null)
        => _path = path ?? Path.Combine(AppPaths.DataDirectory, "credentials.json");

    public StoredCredentials? Load()
    {
        lock (_gate)
            return JsonFileStore.Load<StoredCredentials?>(_path, () => null);
    }

    public void Save(StoredCredentials credentials)
    {
        lock (_gate)
            JsonFileStore.Save(_path, credentials);
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
