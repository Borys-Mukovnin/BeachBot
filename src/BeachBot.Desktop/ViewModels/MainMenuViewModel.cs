using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using BeachBot.Core.Abstractions;
using BeachBot.Core.Models;
using BeachBot.Desktop.Mvvm;
using BeachBot.Desktop.Navigation;

namespace BeachBot.Desktop.ViewModels;

/// <summary>The main menu: scheduled registrations, past registrations, and "New registration".</summary>
public sealed class MainMenuViewModel : ViewModelBase
{
    private readonly IRegistrationService _service;
    private readonly INavigationService _navigation;

    public MainMenuViewModel(IRegistrationService service, INavigationService navigation)
    {
        _service = service;
        _navigation = navigation;

        NewRegistrationCommand = new RelayCommand(() => _navigation.ToTournaments());
        LogoutCommand = BusyCommand(LogoutAsync);
        CancelCommand = new AsyncRelayCommand(CancelAsync, onError: ex => ErrorMessage = ex.Message);
        RefreshCommand = new RelayCommand(Refresh);

        // Background registrations (a scheduled one firing) update the lists; marshal to the UI thread.
        _service.RegistrationsChanged += OnRegistrationsChanged;
    }

    public ObservableCollection<Registration> Scheduled { get; } = new();
    public ObservableCollection<Registration> Past { get; } = new();

    public bool HasScheduled => Scheduled.Count > 0;
    public bool HasPast => Past.Count > 0;

    public ICommand NewRegistrationCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand RefreshCommand { get; }

    public void Refresh()
    {
        Replace(Scheduled, _service.GetScheduledRegistrations());
        Replace(Past, _service.GetPastRegistrations());
        OnPropertyChanged(nameof(HasScheduled));
        OnPropertyChanged(nameof(HasPast));
    }

    private async Task CancelAsync(object? parameter)
    {
        if (parameter is Registration registration)
        {
            await _service.CancelRegistrationAsync(registration.Id);
            Refresh();
        }
    }

    private Task LogoutAsync()
    {
        _navigation.ToLogin();
        return _service.LogoutAsync();
    }

    private void OnRegistrationsChanged(object? sender, EventArgs e)
        => System.Windows.Application.Current?.Dispatcher.Invoke(Refresh);

    private static void Replace(ObservableCollection<Registration> target, IReadOnlyList<Registration> items)
    {
        target.Clear();
        foreach (var item in items)
            target.Add(item);
    }
}
