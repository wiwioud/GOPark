using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkingManagement.Data;
using ParkingManagement.Data.Models;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ParkingManagement.UI.ViewModels
{
    public class LoginViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;

        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private bool _isLoggingIn;
        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set { SetProperty(ref _isLoggingIn, value); }
        }

        // Used for focus state styling
        public bool IsUsernameFocused { get; set; }

        public ICommand LoginCommand { get; }

        public LoginViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => !IsLoggingIn);
        }

        private async Task LoginAsync()
        {
            if (IsLoggingIn) return;
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Bitte Benutzernamen eingeben.";
                return;
            }
            if (string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Bitte Passwort eingeben.";
                return;
            }

            IsLoggingIn = true;
            try
            {
                using var db = App.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
                var user = await db.Users.FirstOrDefaultAsync(u => u.Username == Username);

                if (user == null || !BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
                {
                    ErrorMessage = "Benutzername oder Passwort ist falsch.";
                    return;
                }

                // Write audit log
                db.AuditLogs.Add(new AuditLog
                {
                    UserId = user.Id,
                    Action = "Login",
                    EntityType = "User",
                    EntityId = user.Id.ToString(),
                    Details = $"Benutzer '{user.Username}' hat sich angemeldet.",
                    Timestamp = DateTime.Now
                });
                await db.SaveChangesAsync();

                _mainViewModel.CurrentUser = user;
                _mainViewModel.NavigateTo(new DashboardViewModel(_mainViewModel, user));
            }
            catch (Exception ex)
            {
                ErrorMessage = "Die Datenbankverbindung konnte nicht hergestellt werden.";
                System.Diagnostics.Debug.WriteLine("Login error: " + ex.Message);
            }
            finally
            {
                IsLoggingIn = false;
            }
        }
    }
}
