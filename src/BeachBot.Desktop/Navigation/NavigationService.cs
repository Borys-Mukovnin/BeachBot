using BeachBot.Core.Abstractions;
using BeachBot.Core.Models;
using BeachBot.Desktop.Mvvm;
using BeachBot.Desktop.ViewModels;

namespace BeachBot.Desktop.Navigation;

/// <summary>
/// Creates and swaps view models. The main menu is kept as a single instance (it
/// listens for background registration updates); the flow screens are transient.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly IRegistrationService _service;
    private MainMenuViewModel? _mainMenu;

    public NavigationService(IRegistrationService service) => _service = service;

    public ObservableObject? Current { get; private set; }
    public event Action? Navigated;

    public void ToLogin() => Set(new LoginViewModel(_service, this));

    public void ToMainMenu()
    {
        _mainMenu ??= new MainMenuViewModel(_service, this);
        _mainMenu.Refresh();
        Set(_mainMenu);
    }

    public void ToTournaments() => Set(new TournamentsViewModel(_service, this));

    public void ToChoosePartner(Tournament tournament) => Set(new ChoosePartnerViewModel(_service, this, tournament));

    public void ToConfirmation(Tournament tournament, Player partner) => Set(new ConfirmationViewModel(_service, this, tournament, partner));

    private void Set(ObservableObject viewModel)
    {
        Current = viewModel;
        Navigated?.Invoke();
    }
}
