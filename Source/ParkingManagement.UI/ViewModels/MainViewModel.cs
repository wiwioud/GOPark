using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ParkingManagement.UI.Services;

namespace ParkingManagement.UI.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly NamedPipeClientService _pipeService;
        private readonly DispatcherTimer _timer;

        // =========================================================
        // CURRENT VIEW
        // =========================================================

        private ObservableObject? _currentViewModel;

        public ObservableObject? CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                if (SetProperty(ref _currentViewModel, value))
                {
                    OnPropertyChanged(nameof(CurrentPageTitle));
                }
            }
        }

        public string CurrentPageTitle
        {
            get
            {
                return CurrentViewModel switch
                {
                    DashboardViewModel => "Übersicht",
                    VehicleEntryViewModel => "Einfahrt erfassen",
                    VehicleExitViewModel => "Ausfahrt erfassen",
                    ParkingSpacesViewModel => "Parkplätze",
                    VehiclesViewModel => "Fahrzeuge",
                    ReservationsViewModel => "Reservierungen",
                    ReportsViewModel => "Berichte",
                    UsersViewModel => "Benutzerverwaltung",
                    SettingsViewModel => "Einstellungen",
                    AuditLogViewModel => "Audit-Log",
                    _ => "GOPark"
                };
            }
        }

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public MainViewModel()
        {
            CurrentViewModel = new LoginViewModel(this);

            _pipeService = App.ServiceProvider
                .GetRequiredService<NamedPipeClientService>();

            // Update BOTH the text and the status color
            // whenever the Named Pipe connection changes.
            _pipeService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName ==
                    nameof(NamedPipeClientService.IsConnected))
                {
                    OnPropertyChanged(nameof(ServiceStatus));
                    OnPropertyChanged(nameof(IsServiceOnline));
                }
            };

            _pipeService.OnNotificationReceived += HandleNotification;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _timer.Tick += (s, e) =>
            {
                OnPropertyChanged(nameof(CurrentTimeString));
            };

            _timer.Start();
        }

        // =========================================================
        // TOP BAR
        // =========================================================

        public string CurrentTimeString =>
            DateTime.Now.ToString("dd.MM.yyyy HH:mm");

        public string ServiceStatus =>
            _pipeService.IsConnected
                ? "Online"
                : "Offline";

        public bool IsServiceOnline =>
            _pipeService.IsConnected;

        public bool IsLoggedIn =>
            !(CurrentViewModel is LoginViewModel);

        // =========================================================
        // CURRENT USER
        // =========================================================

        private ParkingManagement.Data.Models.User? _currentUser;

        public ParkingManagement.Data.Models.User? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (SetProperty(ref _currentUser, value))
                {
                    OnPropertyChanged(nameof(IsAdmin));
                    OnPropertyChanged(nameof(UserNameDisplay));
                    OnPropertyChanged(nameof(UserRoleDisplay));
                }
            }
        }

        public bool IsAdmin =>
            CurrentUser?.Role == "Administrator";

        public string UserNameDisplay =>
            CurrentUser?.Username ?? "";

        public string UserRoleDisplay =>
            CurrentUser?.Role ?? "";

        // =========================================================
        // TOAST NOTIFICATION SYSTEM
        // =========================================================

        private string _toastMessage = string.Empty;

        public string ToastMessage
        {
            get => _toastMessage;
            set => SetProperty(ref _toastMessage, value);
        }

        private bool _isToastVisible;

        public bool IsToastVisible
        {
            get => _isToastVisible;
            set => SetProperty(ref _isToastVisible, value);
        }

        public void ShowToast(string message)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(
                async () =>
                {
                    ToastMessage = message;
                    IsToastVisible = true;

                    await Task.Delay(3000);

                    IsToastVisible = false;
                });
        }

        // =========================================================
        // SERVICE NOTIFICATIONS
        // =========================================================

        private void HandleNotification(string message)
        {
            ShowToast(message);
        }

        // =========================================================
        // NAVIGATION
        // =========================================================

        public void NavigateTo(ObservableObject viewModel)
        {
            CurrentViewModel = viewModel;

            OnPropertyChanged(nameof(IsLoggedIn));
        }

        public ICommand GoToDashboardCommand =>
            new RelayCommand(_ =>
            {
                if (CurrentUser != null)
                {
                    NavigateTo(
                        new DashboardViewModel(
                            this,
                            CurrentUser));
                }
            });

        public ICommand GoToEntryCommand =>
            new RelayCommand(_ =>
            {
                if (CurrentUser != null)
                {
                    NavigateTo(
                        new VehicleEntryViewModel(
                            this,
                            CurrentUser));
                }
            });

        public ICommand GoToExitCommand =>
            new RelayCommand(_ =>
            {
                if (CurrentUser != null)
                {
                    NavigateTo(
                        new VehicleExitViewModel(
                            this,
                            CurrentUser));
                }
            });

        public ICommand GoToSpacesCommand =>
            new RelayCommand(_ =>
            {
                if (CurrentUser != null)
                {
                    NavigateTo(
                        new ParkingSpacesViewModel(
                            this,
                            CurrentUser));
                }
            });

        public ICommand GoToVehiclesCommand =>
            new RelayCommand(_ =>
            {
                if (CurrentUser != null)
                {
                    NavigateTo(
                        new VehiclesViewModel(
                            this,
                            CurrentUser));
                }
            });

        public ICommand GoToReservationsCommand =>
            new RelayCommand(_ =>
            {
                if (CurrentUser != null)
                {
                    NavigateTo(
                        new ReservationsViewModel(
                            this,
                            CurrentUser));
                }
            });

        public ICommand GoToReportsCommand =>
            new RelayCommand(_ =>
            {
                if (CurrentUser != null)
                {
                    NavigateTo(
                        new ReportsViewModel(
                            this,
                            CurrentUser));
                }
            });

        // =========================================================
        // ADMIN NAVIGATION
        // =========================================================

        public ICommand GoToUsersCommand =>
            new RelayCommand(_ =>
            {
                if (IsAdmin)
                {
                    NavigateTo(
                        new UsersViewModel(this));
                }
                else
                {
                    ShowToast("Zugriff verweigert.");
                }
            });

        public ICommand GoToSettingsCommand =>
            new RelayCommand(_ =>
            {
                if (IsAdmin)
                {
                    NavigateTo(
                        new SettingsViewModel(this));
                }
                else
                {
                    ShowToast("Zugriff verweigert.");
                }
            });

        public ICommand GoToAuditLogCommand =>
            new RelayCommand(_ =>
            {
                if (IsAdmin)
                {
                    NavigateTo(
                        new AuditLogViewModel(this));
                }
                else
                {
                    ShowToast("Zugriff verweigert.");
                }
            });

        // =========================================================
        // LOGOUT
        // =========================================================

        public ICommand LogoutCommand =>
            new RelayCommand(_ =>
            {
                CurrentUser = null;

                NavigateTo(
                    new LoginViewModel(this));
            });
    }
}