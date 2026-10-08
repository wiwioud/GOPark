using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkingManagement.Data;
using ParkingManagement.Data.Models;
using ParkingManagement.UI.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ParkingManagement.UI.ViewModels
{
    public class ParkingSpacesViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;
        private readonly ParkingStatusService _parkingStatusService;

        public User CurrentUser { get; }

        // =========================================================
        // PARKING SPACES
        // =========================================================

        private ObservableCollection<ParkingSpace> _spaces = new();

        public ObservableCollection<ParkingSpace> Spaces
        {
            get => _spaces;
            set
            {
                SetProperty(ref _spaces, value);
                OnPropertyChanged(nameof(HasNoSpaces));
            }
        }

        public bool HasNoSpaces =>
            _spaces == null || _spaces.Count == 0;

        // =========================================================
        // SELECTED SPACE
        // =========================================================

        private ParkingSpace? _selectedSpace;

        public ParkingSpace? SelectedSpace
        {
            get => _selectedSpace;
            set => SetProperty(ref _selectedSpace, value);
        }

        // =========================================================
        // SEARCH
        // =========================================================

        private string _searchText = string.Empty;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadDataAsync();
                }
            }
        }

        // =========================================================
        // STATUS FILTER
        // =========================================================

        private string _statusFilter = "Alle";

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                if (SetProperty(ref _statusFilter, value))
                {
                    _ = LoadDataAsync();
                }
            }
        }

        // =========================================================
        // COMMANDS
        // =========================================================

        public ICommand GoBackCommand { get; }
        public ICommand LoadDataCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand AddSpaceCommand { get; }
        public ICommand EditSpaceCommand { get; }

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ParkingSpacesViewModel(
            MainViewModel mainViewModel,
            User user)
        {
            _mainViewModel = mainViewModel;
            CurrentUser = user;

            _parkingStatusService = App.ServiceProvider
                .GetRequiredService<ParkingStatusService>();

            GoBackCommand = new RelayCommand(_ =>
                _mainViewModel.NavigateTo(
                    new DashboardViewModel(
                        _mainViewModel,
                        CurrentUser)));

            LoadDataCommand =
                new RelayCommand(
                    async _ => await LoadDataAsync());

            FilterCommand =
                new RelayCommand(param =>
                {
                    if (param is string status)
                    {
                        StatusFilter = status;
                    }
                });

            AddSpaceCommand =
                new RelayCommand(_ => AddSpace());

            EditSpaceCommand =
                new RelayCommand(param =>
                    EditSpace(param as ParkingSpace));

            _ = LoadDataAsync();
        }

        // =========================================================
        // ADD PARKING SPACE
        // =========================================================

        private void AddSpace()
        {
            var window =
                new Views.ParkingSpaceEditWindow();

            var vm =
                new ParkingSpaceEditViewModel(
                    window,
                    _mainViewModel,
                    null);

            window.DataContext = vm;
            window.Owner = Application.Current.MainWindow;

            window.ShowDialog();

            if (vm.WasSaved)
            {
                _ = LoadDataAsync();
            }
        }

        // =========================================================
        // EDIT PARKING SPACE
        // =========================================================

        private void EditSpace(ParkingSpace? space)
        {
            if (space == null)
            {
                return;
            }

            var window =
                new Views.ParkingSpaceEditWindow();

            var vm =
                new ParkingSpaceEditViewModel(
                    window,
                    _mainViewModel,
                    space);

            window.DataContext = vm;
            window.Owner = Application.Current.MainWindow;

            window.ShowDialog();

            if (vm.WasSaved)
            {
                _ = LoadDataAsync();
            }
        }

        // =========================================================
        // LOAD DATA
        // =========================================================

        private async Task LoadDataAsync()
        {
            try
            {
                // First synchronize the real current status of all
                // parking spaces.
                await _parkingStatusService.SyncAllSpacesAsync();

                using var db = App.ServiceProvider
                    .GetRequiredService<IDbContextFactory<AppDbContext>>()
                    .CreateDbContext();

                IQueryable<ParkingSpace> query =
                    db.ParkingSpaces.AsQueryable();

                // =================================================
                // STATUS FILTER
                // =================================================

                if (StatusFilter != "Alle")
                {
                    string dbStatus = StatusFilter switch
                    {
                        "Frei" => "Free",
                        "Belegt" => "Occupied",
                        "Reserviert" => "Reserved",
                        "Außer Betrieb" => "Out of Order",
                        _ => StatusFilter
                    };

                    query = query.Where(
                        s => s.Status == dbStatus);
                }

                // =================================================
                // SEARCH
                // =================================================

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string search =
                        SearchText.Trim();

                    query = query.Where(s =>
                        s.SpaceNumber.Contains(search) ||
                        (s.Zone != null &&
                         s.Zone.Contains(search)) ||
                        (s.Type != null &&
                         s.Type.Contains(search)));
                }

                // =================================================
                // RESULT
                // =================================================

                var list = await query
                    .OrderBy(s => s.SpaceNumber)
                    .ToListAsync();

                Spaces =
                    new ObservableCollection<ParkingSpace>(
                        list);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "ParkingSpaces LoadDataAsync error: " + ex);

                _mainViewModel.ShowToast(
                    "Die Parkplätze konnten nicht geladen werden.");
            }
        }
    }
}