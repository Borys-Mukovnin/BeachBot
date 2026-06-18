using System.Windows;

namespace BeachBot.Desktop;

/// <summary>App entry point: builds the composition root, shows the window, and attempts a silent resume.</summary>
public partial class App : System.Windows.Application
{
    private AppHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = CompositionRoot.Build();

        var window = new MainWindow { DataContext = _host.Shell };
        MainWindow = window;

        // Start on the login screen, then try a silent "remember me" resume in the background.
        _host.Navigation.ToLogin();
        window.Show();

        _ = TryResumeAsync();
    }

    private async Task TryResumeAsync()
    {
        if (_host is null)
            return;
        try
        {
            if (await _host.Service.TryResumeAsync())
                _host.Navigation.ToMainMenu();
        }
        catch
        {
            // Couldn't auto-resume (offline/expired) — the user just logs in normally.
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _host?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch
        {
            // Best-effort cleanup on shutdown.
        }
        base.OnExit(e);
    }
}
