using System.Windows.Input;

namespace BeachBot.Desktop.Mvvm;

/// <summary>
/// Base view model with shared busy/error state and a helper that wraps async work
/// so every command shows progress and reports failures consistently.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    private bool _isBusy;
    private string? _errorMessage;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(_errorMessage);

    /// <summary>Builds an async command that toggles <see cref="IsBusy"/> and captures errors into <see cref="ErrorMessage"/>.</summary>
    protected AsyncRelayCommand BusyCommand(Func<Task> execute, Func<bool>? canExecute = null)
        => new(
            async () =>
            {
                ErrorMessage = null;
                IsBusy = true;
                try { await execute(); }
                finally { IsBusy = false; }
            },
            () => !IsBusy && (canExecute?.Invoke() ?? true),
            ex => ErrorMessage = ex.Message);
}
