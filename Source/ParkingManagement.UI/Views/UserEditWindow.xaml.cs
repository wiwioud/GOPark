using System.Windows;
using System.Windows.Controls;

namespace ParkingManagement.UI.Views
{
    public partial class UserEditWindow : Window
    {
        public UserEditWindow()
        {
            InitializeComponent();
        }

        private void PwBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ParkingManagement.UI.ViewModels.UserEditViewModel vm)
                vm.Password = PwBox.Password;
        }
    }
}
