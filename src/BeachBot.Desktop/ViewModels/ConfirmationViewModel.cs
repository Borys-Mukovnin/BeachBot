using System.Windows.Input;
using BeachBot.Core.Abstractions;
using BeachBot.Core.Models;
using BeachBot.Desktop.Mvvm;
using BeachBot.Desktop.Navigation;

namespace BeachBot.Desktop.ViewModels;

/// <summary>
/// "Display confirmation": shows everything about the registration to schedule and
/// requires an explicit confirm before it is queued.
/// </summary>
public sealed class ConfirmationViewModel : ViewModelBase
{
    private readonly IRegistrationService _service;
    private readonly INavigationService _navigation;
    private readonly Tournament _tournament;
    private readonly Player _partner;

    public ConfirmationViewModel(IRegistrationService service, INavigationService navigation, Tournament tournament, Player partner)
    {
        _service = service;
        _navigation = navigation;
        _tournament = tournament;
        _partner = partner;

        ConfirmCommand = BusyCommand(ConfirmAsync);
        CancelCommand = new RelayCommand(() => _navigation.ToMainMenu());
    }

    public string TournamentName => _tournament.Name;
    public string City => _tournament.City;
    public string Series => _tournament.SeriesName ?? _tournament.SeriesId ?? "—";
    public string Category => _tournament.CategoryName ?? _tournament.CategoryId ?? "—";
    public string TeamSizeText => $"{_tournament.TeamSize} players";

    public DateTimeOffset? GameDate => _tournament.GameDate;
    public DateTimeOffset? RegistrationOpensAt => _tournament.RegistrationOpensAt;

    public string PartnerName => _partner.FullName;
    public string PartnerClub => _partner.Club ?? "—";

    public bool RegistrationAlreadyOpen =>
        _tournament.RegistrationOpensAt is null || _tournament.RegistrationOpensAt <= DateTimeOffset.UtcNow;

    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }

    private async Task ConfirmAsync()
    {
        await _service.ScheduleRegistrationAsync(_tournament, _partner);
        _navigation.ToMainMenu();
    }
}
