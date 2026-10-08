using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkingManagement.Data;
using ParkingManagement.Data.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ParkingManagement.UI.ViewModels
{
    public class UsersViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;

        private ObservableCollection<User> _users = new();
        public ObservableCollection<User> Users
        {
            get => _users;
            set { SetProperty(ref _users, value); OnPropertyChanged(nameof(HasNoUsers)); }
        }
        public bool HasNoUsers => _users == null || _users.Count == 0;

        public ICommand GoBackCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) _ = LoadDataAsync(); }
        }

        public UsersViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            GoBackCommand = new RelayCommand(_ => _mainViewModel.NavigateTo(new DashboardViewModel(_mainViewModel, _mainViewModel.CurrentUser!)));
            RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());
            AddUserCommand = new RelayCommand(_ => AddUser());
            EditUserCommand = new RelayCommand(param => EditUser(param as User));

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            using var db = App.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
            var query = db.Users.AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchText))
                query = query.Where(u => u.Username.Contains(SearchText) || u.Role.Contains(SearchText));
            var list = await query.OrderBy(u => u.Username).ToListAsync();
            Application.Current.Dispatcher.Invoke(() => Users = new ObservableCollection<User>(list));
        }

        private void AddUser()
        {
            var window = new Views.UserEditWindow();
            var vm = new UserEditViewModel(window, _mainViewModel, null);
            window.DataContext = vm;
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
            if (vm.WasSaved) _ = LoadDataAsync();
        }

        private void EditUser(User? user)
        {
            if (user == null) return;
            var window = new Views.UserEditWindow();
            var vm = new UserEditViewModel(window, _mainViewModel, user);
            window.DataContext = vm;
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
            if (vm.WasSaved) _ = LoadDataAsync();
        }
    }
}
