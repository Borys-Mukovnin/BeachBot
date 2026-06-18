using BeachBot.Api;
using BeachBot.Api.Auth;
using BeachBot.Api.Ddp;
using BeachBot.Application;
using BeachBot.Application.Storage;
using BeachBot.Core.Abstractions;
using BeachBot.Desktop.Navigation;
using BeachBot.Desktop.ViewModels;

namespace BeachBot.Desktop;

/// <summary>
/// Manual composition root — the whole object graph wired by hand. Simple and
/// explicit, with no DI container (per the "no over-complex patterns" guideline).
/// </summary>
public static class CompositionRoot
{
    public static AppHost Build()
    {
        var connection = new DdpConnection(new DdpOptions());
        IAuthenticator authenticator = new Authenticator(connection);
        ITournamentApi api = new TournamentApiClient(connection);
        IClock clock = new SystemClock();
        var scheduler = new RegistrationScheduler(clock);

        var credentials = new CredentialStore();
        var registrations = new RegistrationStore();
        var partners = new PlayerProfileStore();

        var service = new RegistrationService(
            connection, authenticator, api, clock, scheduler, credentials, registrations, partners);

        var navigation = new NavigationService(service);
        var shell = new ShellViewModel(navigation, service);

        return new AppHost(shell, service, navigation, service);
    }
}

/// <summary>Holds the wired-up top-level objects and owns their disposal.</summary>
public sealed class AppHost : IAsyncDisposable
{
    private readonly IAsyncDisposable _disposable;

    public AppHost(ShellViewModel shell, IRegistrationService service, INavigationService navigation, IAsyncDisposable disposable)
    {
        Shell = shell;
        Service = service;
        Navigation = navigation;
        _disposable = disposable;
    }

    public ShellViewModel Shell { get; }
    public IRegistrationService Service { get; }
    public INavigationService Navigation { get; }

    public ValueTask DisposeAsync() => _disposable.DisposeAsync();
}
