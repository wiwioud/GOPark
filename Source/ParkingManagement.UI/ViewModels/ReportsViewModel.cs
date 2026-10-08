using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkingManagement.Data;
using ParkingManagement.Data.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using System;
using System.IO;
using System.Text;

namespace ParkingManagement.UI.ViewModels
{
    public class ReportsViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;
        public User CurrentUser { get; }

        private ObservableCollection<ParkingSession> _completedSessions = new();
        public ObservableCollection<ParkingSession> CompletedSessions
        {
            get => _completedSessions;
            set { SetProperty(ref _completedSessions, value); OnPropertyChanged(nameof(HasNoSessions)); }
        }
        public bool HasNoSessions => _completedSessions == null || _completedSessions.Count == 0;

        public ICommand GoBackCommand { get; }
        public ICommand ExportCsvCommand { get; }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) _ = LoadDataAsync(); }
        }

        private string _statusFilter = "Alle";
        public string StatusFilter
        {
            get => _statusFilter;
            set { if (SetProperty(ref _statusFilter, value)) _ = LoadDataAsync(); }
        }

        private string _dateFilter = "Alle Zeiten";
        public string DateFilter
        {
            get => _dateFilter;
            set { if (SetProperty(ref _dateFilter, value)) _ = LoadDataAsync(); }
        }

        public ICommand FilterStatusCommand { get; }
        public ICommand FilterDateCommand { get; }

        public ReportsViewModel(MainViewModel mainViewModel, User user)
        {
            _mainViewModel = mainViewModel;
            CurrentUser = user;

            GoBackCommand = new RelayCommand(_ => _mainViewModel.NavigateTo(new DashboardViewModel(_mainViewModel, CurrentUser)));
            ExportCsvCommand = new RelayCommand(async _ => await ExportCsvAsync());
            FilterStatusCommand = new RelayCommand(param => { if (param is string s) StatusFilter = s; });
            FilterDateCommand = new RelayCommand(param => { if (param is string s) DateFilter = s; });

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            using var db = App.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
            var query = db.ParkingSessions
                .Include(s => s.Vehicle)
                .Include(s => s.ParkingSpace)
                .AsQueryable();

            // Status filter
            if (StatusFilter == "Aktiv")
                query = query.Where(s => s.Status == "Active");
            else if (StatusFilter == "Abgeschlossen")
                query = query.Where(s => s.Status == "Completed");
            // else "Alle" = all sessions

            // Date filter
            var today = DateTime.Today;
            if (DateFilter == "Heute")
                query = query.Where(s => s.EntryTime >= today && s.EntryTime < today.AddDays(1));
            else if (DateFilter == "Diese Woche")
            {
                var weekStart = today.AddDays(-(int)today.DayOfWeek);
                query = query.Where(s => s.EntryTime >= weekStart);
            }
            else if (DateFilter == "Dieser Monat")
            {
                var monthStart = new DateTime(today.Year, today.Month, 1);
                query = query.Where(s => s.EntryTime >= monthStart);
            }

            // Text search
            if (!string.IsNullOrWhiteSpace(SearchText))
                query = query.Where(s => s.Vehicle.LicensePlate.Contains(SearchText) || s.ParkingSpace.SpaceNumber.Contains(SearchText));

            var list = await query.OrderByDescending(s => s.EntryTime).Take(200).ToListAsync();
            CompletedSessions = new ObservableCollection<ParkingSession>(list);
        }

        private async Task ExportCsvAsync()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Kennzeichen;Parkplatz;Einfahrt;Ausfahrt;Dauer;Gebühr");

                foreach (var session in CompletedSessions)
                {
                    var duration = (session.ExitTime - session.EntryTime)?.ToString(@"hh\:mm") ?? "--";
                    sb.AppendLine($"{session.Vehicle?.LicensePlate};{session.ParkingSpace?.SpaceNumber};{session.EntryTime:dd.MM.yyyy HH:mm};{session.ExitTime:dd.MM.yyyy HH:mm};{duration};{session.ParkingFee:N2}");
                }

                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Report_{DateTime.Now:yyyyMMdd_HHmm}.csv");
                await File.WriteAllTextAsync(path, sb.ToString(), System.Text.Encoding.UTF8);
                _mainViewModel.ShowToast($"Exportiert nach Desktop: Report_{DateTime.Now:yyyyMMdd_HHmm}.csv");
            }
            catch (Exception ex)
            {
                _mainViewModel.ShowToast("Fehler beim Exportieren: " + ex.Message);
            }
        }
    }
}
