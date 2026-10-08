using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkingManagement.Data;
using ParkingManagement.Data.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ParkingManagement.UI.ViewModels
{
    public class UserEditViewModel : ObservableObject
    {
        private readonly Window _window;
        private readonly MainViewModel _mainViewModel;
        private readonly User? _existingUser;

        public string Title => _existingUser == null ? "Neuen Benutzer anlegen" : "Benutzer bearbeiten";
        public bool WasSaved { get; private set; } = false;
        public bool IsNewUser => _existingUser == null;

        public List<string> RoleOptions { get; } = new() { "Administrator", "Employee" };

        private string _username = string.Empty;
        public string Username { get => _username; set => SetProperty(ref _username, value); }

        private string _password = string.Empty;
        public string Password { get => _password; set => SetProperty(ref _password, value); }

        private string _selectedRole = "Employee";
        public string SelectedRole { get => _selectedRole; set => SetProperty(ref _selectedRole, value); }

        private string _errorMessage = string.Empty;
        public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public UserEditViewModel(Window window, MainViewModel mainViewModel, User? existing)
        {
            _window = window;
            _mainViewModel = mainViewModel;
            _existingUser = existing;

            if (existing != null)
            {
                Username = existing.Username;
                SelectedRole = existing.Role;
            }

            SaveCommand = new RelayCommand(async _ => await SaveAsync());
            CancelCommand = new RelayCommand(_ => _window.Close());
        }

        private async Task SaveAsync()
        {
            ErrorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(Username)) { ErrorMessage = "Benutzername ist erforderlich."; return; }
            if (Username.Length < 3) { ErrorMessage = "Benutzername muss mindestens 3 Zeichen haben."; return; }
            if (_existingUser == null && string.IsNullOrEmpty(Password)) { ErrorMessage = "Passwort ist erforderlich."; return; }
            if (!string.IsNullOrEmpty(Password) && Password.Length < 6) { ErrorMessage = "Passwort muss mindestens 6 Zeichen haben."; return; }

            try
            {
                using var db = App.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();

                if (_existingUser == null)
                {
                    // Check duplicate username
                    bool exists = await db.Users.AnyAsync(u => u.Username == Username);
                    if (exists) { ErrorMessage = "Dieser Benutzername ist bereits vergeben."; return; }

                    db.Users.Add(new User
                    {
                        Username = Username,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                        Role = SelectedRole
                    });
                }
                else
                {
                    var user = await db.Users.FindAsync(_existingUser.Id);
                    if (user == null) { ErrorMessage = "Benutzer nicht gefunden."; return; }

                    // Check duplicate username (exclude current)
                    bool exists = await db.Users.AnyAsync(u => u.Username == Username && u.Id != _existingUser.Id);
                    if (exists) { ErrorMessage = "Dieser Benutzername ist bereits vergeben."; return; }

                    // Prevent removing the last admin
                    if (user.Role == "Administrator" && SelectedRole != "Administrator")
                    {
                        var adminCount = await db.Users.CountAsync(u => u.Role == "Administrator");
                        if (adminCount <= 1) { ErrorMessage = "Der letzte Administrator kann nicht herabgestuft werden."; return; }
                    }

                    user.Username = Username;
                    user.Role = SelectedRole;
                    if (!string.IsNullOrEmpty(Password))
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);
                }

                await db.SaveChangesAsync();
                WasSaved = true;
                _window.Close();
                _mainViewModel.ShowToast(_existingUser == null ? "Benutzer wurde angelegt." : "Benutzer wurde gespeichert.");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Fehler: " + ex.Message;
            }
        }
    }
}
