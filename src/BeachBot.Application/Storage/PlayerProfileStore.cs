using BeachBot.Core.Models;

namespace BeachBot.Application.Storage;

/// <summary>Your saved partners — players reused across registrations, shown as quick picks.</summary>
public interface IPlayerProfileStore
{
    IReadOnlyList<Player> GetPartners();
    void AddPartner(Player player);
    void RemovePartner(string playerId);
}

/// <summary>File-backed list of previously used partners (most recent first).</summary>
public sealed class PlayerProfileStore : IPlayerProfileStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private readonly List<Player> _partners;

    public PlayerProfileStore(string? path = null)
    {
        _path = path ?? Path.Combine(AppPaths.DataDirectory, "partners.json");
        _partners = JsonFileStore.Load(_path, () => new List<Player>());
    }

    public IReadOnlyList<Player> GetPartners()
    {
        lock (_gate)
            return _partners.ToList();
    }

    public void AddPartner(Player player)
    {
        lock (_gate)
        {
            _partners.RemoveAll(p => p.Id == player.Id);
            _partners.Insert(0, player); // most recently used first
            JsonFileStore.Save(_path, _partners);
        }
    }

    public void RemovePartner(string playerId)
    {
        lock (_gate)
        {
            if (_partners.RemoveAll(p => p.Id == playerId) > 0)
                JsonFileStore.Save(_path, _partners);
        }
    }
}
