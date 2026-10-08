using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkingManagement.Data;
using ParkingManagement.Data.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;

namespace ParkingManagement.UI.ViewModels
{
    public class VehiclesViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;
        public User CurrentUser { get; }

        private ObservableCollection<Vehicle> _vehicles = new();
        public ObservableCollection<Vehicle> Vehicles
        {
            get => _vehicles;
            set => SetProperty(ref _vehicles, value);
        }

        
                private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set 
            { 
                SetProperty(ref _searchText, value);
                _ = SearchVehiclesAsync();
            }
        }

                public ICommand GoBackCommand { get; }
                public ICommand AddVehicleCommand { get; }
        public ICommand EditVehicleCommand { get; }
        public ICommand SearchCommand { get; }

        public VehiclesViewModel(MainViewModel mainViewModel, User user)
        {
            _mainViewModel = mainViewModel;
            CurrentUser = user;
            
                        GoBackCommand = new RelayCommand(_ => _mainViewModel.NavigateTo(new DashboardViewModel(_mainViewModel, CurrentUser)));
                        AddVehicleCommand = new RelayCommand(_ => AddVehicle());
            EditVehicleCommand = new RelayCommand(obj => EditVehicle(obj as Vehicle));
            SearchCommand = new RelayCommand(_ => { _ = SearchVehiclesAsync(); });
            _ = LoadDataAsync();
        }

                private void AddVehicle()
        {
            var window = new Views.VehicleEditWindow();
            var vm = new VehicleEditViewModel(window, _mainViewModel);
            window.DataContext = vm;
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
            if (vm.WasSaved)
            {
                _ = LoadDataAsync();
            }
        }

                private void EditVehicle(Vehicle vehicle)
        {
            if (vehicle == null) return;
            var window = new Views.VehicleEditWindow();
            var vm = new VehicleEditViewModel(window, _mainViewModel, vehicle);
            window.DataContext = vm;
            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
            if (vm.WasSaved)
            {
                _ = LoadDataAsync();
            }
        }

        private async Task LoadDataAsync()
        {
            using var db = App.ServiceProvider.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<AppDbContext>>().CreateDbContext();
            var list = await db.Vehicles.OrderBy(v => v.LicensePlate).ToListAsync();
            Vehicles = new ObservableCollection<Vehicle>(list);
        }
        
        private async Task SearchVehiclesAsync()
        {
            using var db = App.ServiceProvider.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<AppDbContext>>().CreateDbContext();
            var query = db.Vehicles.AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(v => v.LicensePlate.Contains(SearchText) || v.Make.Contains(SearchText) || v.Model.Contains(SearchText) || v.OwnerName.Contains(SearchText));
            }
            var vList = await query.OrderBy(v => v.LicensePlate).ToListAsync();
            Vehicles = new ObservableCollection<Vehicle>(vList);
        }
    }
}



