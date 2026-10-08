using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkingManagement.Data;
using ParkingManagement.Data.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ParkingManagement.UI.ViewModels
{
    public class AuditLogViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;

        private ObservableCollection<AuditLog> _logs = new();
        public ObservableCollection<AuditLog> Logs
        {
            get => _logs;
            set { SetProperty(ref _logs, value); OnPropertyChanged(nameof(HasNoLogs)); }
        }
        public bool HasNoLogs => _logs == null || _logs.Count == 0;

        public ICommand GoBackCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand FilterCommand { get; }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) _ = LoadDataAsync(); }
        }

        private string _actionFilter = "Alle";
        public string ActionFilter
        {
            get => _actionFilter;
            set { if (SetProperty(ref _actionFilter, value)) _ = LoadDataAsync(); }
        }

        public AuditLogViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            GoBackCommand = new RelayCommand(_ => _mainViewModel.NavigateTo(new DashboardViewModel(_mainViewModel, _mainViewModel.CurrentUser!)));
            ExportCsvCommand = new RelayCommand(async _ => await ExportCsvAsync());
            FilterCommand = new RelayCommand(param => { if (param is string s) ActionFilter = s; });

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            using var db = App.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();

            var query = db.AuditLogs.Include(a => a.User).AsQueryable();

            // Action filter
            if (ActionFilter != "Alle")
                query = query.Where(a => a.Action == ActionFilter);

            // Text search
            if (!string.IsNullOrWhiteSpace(SearchText))
                query = query.Where(a =>
                    (a.User != null && a.User.Username.Contains(SearchText)) ||
                    a.Action.Contains(SearchText) ||
                    a.EntityType.Contains(SearchText) ||
                    a.Details.Contains(SearchText));

            var logList = await query
                .OrderByDescending(a => a.Timestamp)
                .Take(300)
                .ToListAsync();

            Application.Current.Dispatcher.Invoke(() =>
                Logs = new ObservableCollection<AuditLog>(logList));
        }

        private async Task ExportCsvAsync()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Zeitpunkt;Benutzer;Aktion;Bereich;Details");

                foreach (var log in Logs)
                {
                    sb.AppendLine($"{log.Timestamp:dd.MM.yyyy HH:mm:ss};{log.User?.Username ?? "-"};{log.Action};{log.EntityType};{log.Details}");
                }

                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"AuditLog_{DateTime.Now:yyyyMMdd_HHmm}.csv");
                await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(true));
                _mainViewModel.ShowToast($"Exportiert: {Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                _mainViewModel.ShowToast("Fehler beim Exportieren: " + ex.Message);
            }
        }
    }
}
