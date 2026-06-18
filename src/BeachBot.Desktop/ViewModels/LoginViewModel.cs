using System.Windows.Input;
using BeachBot.Core.Abstractions;
using BeachBot.Desktop.Mvvm;
using BeachBot.Desktop.Navigation;

namespace BeachBot.Desktop.ViewModels;

/// <summary>The "Authenticate" step. Supports "Remember me" for silent re-login next time.</summary>
public sealed class LoginViewModel : ViewModelBase
{
    private readonly IRegistrationService _service;
    private readonly INavigationService _navigation;

    private string _email = "";
    private string _password = "";
    private bool _rememberMe = true;

    public LoginViewModel(IRegistrationService service, INavigationService navigation)
    {
        _service = service;
        _navigation = navigation;
        LoginCommand = BusyCommand(LoginAsync, CanLogin);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    // Bound from the view's PasswordBox (PasswordBox.Password isn't directly bindable).
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public bool RememberMe
    {
        get => _rememberMe;
        set => SetProperty(ref _rememberMe, value);
    }

    public ICommand LoginCommand { get; }

    private bool CanLogin() => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrEmpty(Password);

    private async Task LoginAsync()
    {
        await _service.LoginAsync(Email.Trim(), Password, RememberMe);
        _navigation.ToMainMenu();
    }
}
