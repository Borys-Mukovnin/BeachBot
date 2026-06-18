using System.Windows;
using System.Windows.Controls;
using BeachBot.Desktop.ViewModels;

namespace BeachBot.Desktop.Views;

public partial class LoginView : UserControl
{
    public LoginView() => InitializeComponent();

    // PasswordBox.Password isn't bindable; push it into the view model on change.
    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is PasswordBox box)
            vm.Password = box.Password;
    }
}
