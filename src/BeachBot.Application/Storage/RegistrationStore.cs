using BeachBot.Core.Models;

namespace BeachBot.Application.Storage;

/// <summary>Persists scheduled and past registrations so they survive app restarts.</summary>
public interface IRegistrationStore
{
    IReadOnlyList<Registration> GetAll();
    void Upsert(Registration registration);
    void Remove(string id);
}

/// <summary>File-backed registration list.</summary>
public sealed class RegistrationStore : IRegistrationStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private readonly List<Registration> _registrations;

    public RegistrationStore(string? path = null)
    {
        _path = path ?? Path.Combine(AppPaths.DataDirectory, "registrations.json");
        _registrations = JsonFileStore.Load(_path, () => new List<Registration>());
    }

    public IReadOnlyList<Registration> GetAll()
    {
        lock (_gate)
            return _registrations.ToList();
    }

    public void Upsert(Registration registration)
    {
        lock (_gate)
        {
            _registrations.RemoveAll(r => r.Id == registration.Id);
            _registrations.Add(registration);
            JsonFileStore.Save(_path, _registrations);
        }
    }

    public void Remove(string id)
    {
        lock (_gate)
        {
            if (_registrations.RemoveAll(r => r.Id == id) > 0)
                JsonFileStore.Save(_path, _registrations);
        }
    }
}
