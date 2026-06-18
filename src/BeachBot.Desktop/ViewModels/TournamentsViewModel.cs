using System.Collections.ObjectModel;
using System.Windows.Input;
using BeachBot.Application;
using BeachBot.Core.Abstractions;
using BeachBot.Core.Models;
using BeachBot.Desktop.Mvvm;
using BeachBot.Desktop.Navigation;

namespace BeachBot.Desktop.ViewModels;

/// <summary>A series/category dropdown entry. A null <see cref="Id"/> means "all".</summary>
public sealed record FilterOption(string? Id, string Label)
{
    public override string ToString() => Label;
}

/// <summary>
/// "Display tournaments": shows tournaments where registration has not opened yet,
/// with filters for city (free text), series, and category (dropdowns).
/// </summary>
public sealed class TournamentsViewModel : ViewModelBase
{
    private readonly IRegistrationService _service;
    private readonly INavigationService _navigation;

    private IReadOnlyList<Tournament> _all = Array.Empty<Tournament>();

    private string _cityFilter = "";
    private FilterOption _selectedSeries = AllOption;
    private FilterOption _selectedCategory = AllOption;
    private bool _onlyNotYetOpen;

    private static readonly FilterOption AllOption = new(null, "All");

    public TournamentsViewModel(IRegistrationService service, INavigationService navigation)
    {
        _service = service;
        _navigation = navigation;

        LoadCommand = BusyCommand(LoadAsync);
        SelectCommand = new AsyncRelayCommand(SelectAsync, onError: ex => ErrorMessage = ex.Message);
        BackCommand = new RelayCommand(() => _navigation.ToMainMenu());

        SeriesOptions.Add(AllOption);
        CategoryOptions.Add(AllOption);

        // Load as soon as the screen appears.
        LoadCommand.Execute(null);
    }

    public ObservableCollection<Tournament> Tournaments { get; } = new();
    public ObservableCollection<FilterOption> SeriesOptions { get; } = new();
    public ObservableCollection<FilterOption> CategoryOptions { get; } = new();

    public bool IsEmpty => !IsBusy && Tournaments.Count == 0;

    public int ResultCount => Tournaments.Count;

    /// <summary>When on, restrict to tournaments whose registration hasn't opened yet. Off by default — show everything.</summary>
    public bool OnlyNotYetOpen
    {
        get => _onlyNotYetOpen;
        set { if (SetProperty(ref _onlyNotYetOpen, value)) ApplyFilter(); }
    }

    public string CityFilter
    {
        get => _cityFilter;
        set { if (SetProperty(ref _cityFilter, value)) ApplyFilter(); }
    }

    public FilterOption SelectedSeries
    {
        get => _selectedSeries;
        set { if (SetProperty(ref _selectedSeries, value ?? AllOption)) ApplyFilter(); }
    }

    public FilterOption SelectedCategory
    {
        get => _selectedCategory;
        set { if (SetProperty(ref _selectedCategory, value ?? AllOption)) ApplyFilter(); }
    }

    public ICommand LoadCommand { get; }
    public ICommand SelectCommand { get; }
    public ICommand BackCommand { get; }

    private async Task LoadAsync()
    {
        _all = await _service.GetUpcomingTournamentsAsync();
        BuildFilterOptions();
        ApplyFilter();
    }

    private async Task SelectAsync(object? parameter)
    {
        if (parameter is not Tournament tournament)
            return;

        // Fetch the detail so we know exactly when registration opens before confirming.
        var detail = await _service.GetTournamentDetailAsync(tournament.Id);
        _navigation.ToChoosePartner(detail);
    }

    private void BuildFilterOptions()
    {
        RebuildOptions(SeriesOptions, _all.Select(t => (t.SeriesId, t.SeriesName)));
        RebuildOptions(CategoryOptions, _all.Select(t => (t.CategoryId, t.CategoryName)));
        _selectedSeries = AllOption;
        _selectedCategory = AllOption;
        OnPropertyChanged(nameof(SelectedSeries));
        OnPropertyChanged(nameof(SelectedCategory));
    }

    private static void RebuildOptions(ObservableCollection<FilterOption> target, IEnumerable<(string? Id, string? Name)> source)
    {
        target.Clear();
        target.Add(AllOption);
        foreach (var option in source
                     .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                     .Select(x => new FilterOption(x.Id, string.IsNullOrWhiteSpace(x.Name) ? x.Id! : x.Name!))
                     .DistinctBy(o => o.Id)
                     .OrderBy(o => o.Label))
            target.Add(option);
    }

    private void ApplyFilter()
    {
        var criteria = new TournamentFilterCriteria(
            City: CityFilter,
            SeriesId: SelectedSeries.Id,
            CategoryId: SelectedCategory.Id,
            OnlyRegistrationNotOpen: OnlyNotYetOpen);

        var filtered = TournamentFilter.Apply(_all, criteria, DateTimeOffset.UtcNow)
            .OrderBy(t => t.GameDate ?? DateTimeOffset.MaxValue)
            .ToList();

        Tournaments.Clear();
        foreach (var tournament in filtered)
            Tournaments.Add(tournament);
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(ResultCount));
    }
}
