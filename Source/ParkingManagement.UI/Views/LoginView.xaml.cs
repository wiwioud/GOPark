using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ParkingManagement.UI.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            Loaded += (s, e) => UsernameBox?.Focus();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ParkingManagement.UI.ViewModels.LoginViewModel vm)
                vm.Password = PasswordBox.Password;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) PasswordBox?.Focus();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is ParkingManagement.UI.ViewModels.LoginViewModel vm)
                    vm.LoginCommand.Execute(null);
            }
        }
    }
}
