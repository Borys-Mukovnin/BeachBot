using BeachBot.Core.Models;
using BeachBot.Desktop.Mvvm;

namespace BeachBot.Desktop.Navigation;

/// <summary>
/// Drives the single-window flow from the activity diagram:
/// Login → Main menu → Tournaments → Choose partner → Confirmation.
/// </summary>
public interface INavigationService
{
    ObservableObject? Current { get; }
    event Action? Navigated;

    void ToLogin();
    void ToMainMenu();
    void ToTournaments();
    void ToChoosePartner(Tournament tournament);
    void ToConfirmation(Tournament tournament, Player partner);
}
