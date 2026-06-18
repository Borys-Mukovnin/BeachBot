using BeachBot.Core.Abstractions;
using BeachBot.Desktop.Mvvm;
using BeachBot.Desktop.Navigation;

namespace BeachBot.Desktop.ViewModels;

/// <summary>Top-level view model: hosts the current screen and the header status.</summary>
public sealed class ShellViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly IRegistrationService _service;

    public ShellViewModel(INavigationService navigation, IRegistrationService service)
    {
        _navigation = navigation;
        _service = service;
        _navigation.Navigated += OnNavigated;
    }

    public object? CurrentViewModel => _navigation.Current;

    public bool IsAuthenticated => _service.IsAuthenticated;

    public string UserStatus => _service.IsAuthenticated
        ? $"Signed in as {_service.CurrentUserEmail}"
        : "Not signed in";

    private void OnNavigated()
    {
        OnPropertyChanged(nameof(CurrentViewModel));
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(UserStatus));
    }
}
