using System.Collections.ObjectModel;
using System.Windows.Input;
using BeachBot.Core.Abstractions;
using BeachBot.Core.Models;
using BeachBot.Desktop.Mvvm;
using BeachBot.Desktop.Navigation;

namespace BeachBot.Desktop.ViewModels;

/// <summary>
/// "Choose a player": searches partners by name and offers previously used
/// partners as quick picks (each removable with an x).
/// </summary>
public sealed class ChoosePartnerViewModel : ViewModelBase
{
    private readonly IRegistrationService _service;
    private readonly INavigationService _navigation;
    private readonly Tournament _tournament;

    private string _searchText = "";

    public ChoosePartnerViewModel(IRegistrationService service, INavigationService navigation, Tournament tournament)
    {
        _service = service;
        _navigation = navigation;
        _tournament = tournament;

        SearchCommand = BusyCommand(SearchAsync, () => SearchText.Trim().Length >= 2);
        SelectCommand = new RelayCommand(p => SelectPartner(p as Player));
        RemoveSavedCommand = new RelayCommand(p => RemoveSaved(p as Player));
        BackCommand = new RelayCommand(() => _navigation.ToTournaments());

        LoadSaved();
    }

    public string TournamentName => _tournament.Name;

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public ObservableCollection<Player> SearchResults { get; } = new();
    public ObservableCollection<Player> SavedPartners { get; } = new();

    public bool HasSavedPartners => SavedPartners.Count > 0;

    public ICommand SearchCommand { get; }
    public ICommand SelectCommand { get; }
    public ICommand RemoveSavedCommand { get; }
    public ICommand BackCommand { get; }

    private async Task SearchAsync()
    {
        var results = await _service.SearchPlayersAsync(SearchText.Trim(), _tournament.SeriesId);
        SearchResults.Clear();
        foreach (var player in results)
            SearchResults.Add(player);
    }

    private void SelectPartner(Player? player)
    {
        if (player is not null)
            _navigation.ToConfirmation(_tournament, player);
    }

    private void RemoveSaved(Player? player)
    {
        if (player is null)
            return;
        _service.RemoveSavedPartner(player.Id);
        LoadSaved();
    }

    private void LoadSaved()
    {
        SavedPartners.Clear();
        foreach (var player in _service.GetSavedPartners())
            SavedPartners.Add(player);
        OnPropertyChanged(nameof(HasSavedPartners));
    }
}
