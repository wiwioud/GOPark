using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ParkingManagement.Data;
using ParkingManagement.Data.Database;
using ParkingManagement.UI.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace ParkingManagement.UI
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;
        public static IConfiguration Configuration { get; private set; } = null!;

        private System.Diagnostics.Process? _workerProcess;

        // =========================================================
        // APP CONSTRUCTOR
        // =========================================================

        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        // =========================================================
        // GLOBAL ERROR LOGGING
        // =========================================================

        private void LogException(Exception ex, string source)
        {
            try
            {
                var logDir = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Logs");

                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                var logFile = Path.Combine(
                    logDir,
                    "crash.log");

                var message =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                    $"[{source}] " +
                    $"{ex.GetType().Name}: {ex.Message}\n" +
                    $"{ex.StackTrace}\n\n";

                File.AppendAllText(logFile, message);
            }
            catch
            {
                // Logging must never crash the application.
            }
        }

        private void App_DispatcherUnhandledException(
            object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(
                e.Exception,
                "DispatcherUnhandledException");

            MessageBox.Show(
                "Ein unerwarteter Fehler ist aufgetreten. " +
                "Details wurden im Logs-Ordner gespeichert: " +
                e.Exception.Message,
                "Fehler",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(
            object sender,
            UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException(
                    ex,
                    "CurrentDomain_UnhandledException");
            }
        }

        private void TaskScheduler_UnobservedTaskException(
            object? sender,
            UnobservedTaskExceptionEventArgs e)
        {
            LogException(
                e.Exception,
                "TaskScheduler_UnobservedTaskException");

            e.SetObserved();
        }

        // =========================================================
        // APPLICATION STARTUP
        // =========================================================

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // -----------------------------------------------------
            // Configuration
            // -----------------------------------------------------

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile(
                    "appsettings.json",
                    optional: false,
                    reloadOnChange: true);

            Configuration = builder.Build();

            // -----------------------------------------------------
            // Dependency Injection
            // -----------------------------------------------------

            var serviceCollection =
                new ServiceCollection();

            ConfigureServices(serviceCollection);

            ServiceProvider =
                serviceCollection.BuildServiceProvider();

            // -----------------------------------------------------
            // Database Initialization
            // -----------------------------------------------------

            using (var scope =
                   ServiceProvider.CreateScope())
            {
                var factory =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IDbContextFactory<AppDbContext>>();

                using var dbContext =
                    factory.CreateDbContext();

                try
                {
                    await DbInitializer
                        .InitializeAsync(dbContext);
                }
                catch (Exception ex)
                {
                    LogException(
                        ex,
                        "DatabaseInitialization");

                    MessageBox.Show(
                        "Die Datenbank konnte nicht initialisiert werden:\n\n" +
                        ex.Message,
                        "Datenbankfehler",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }

            // -----------------------------------------------------
            // Start Worker Service
            // -----------------------------------------------------

            StartWorkerService();

            // -----------------------------------------------------
            // Start Named Pipe Client
            // -----------------------------------------------------

            var pipeClient =
                ServiceProvider
                    .GetRequiredService<NamedPipeClientService>();

            _ = Task.Run(
                () => pipeClient.StartListeningAsync());

            // -----------------------------------------------------
            // Show Main Window
            // -----------------------------------------------------

            var mainWindow =
                new MainWindow();

            mainWindow.Show();
        }

        // =========================================================
        // DEPENDENCY INJECTION
        // =========================================================

        private void ConfigureServices(
            IServiceCollection services)
        {
            var connectionString =
                Configuration.GetConnectionString(
                    "DefaultConnection");

            services.AddDbContextFactory<AppDbContext>(
                options =>
                    options.UseSqlServer(
                        connectionString));

            services.AddSingleton<
                NamedPipeClientService>();

            services.AddSingleton<
                ParkingFeeCalculator>();

            services.AddSingleton<
                ParkingStatusService>();

            services.AddTransient<MainWindow>();
        }

        // =========================================================
        // WORKER SERVICE
        // =========================================================

        private void StartWorkerService()
        {
            try
            {
                // -------------------------------------------------
                // Prevent duplicate Worker instances
                // -------------------------------------------------

                var existingProcesses =
                    System.Diagnostics.Process
                        .GetProcessesByName(
                            "ParkingMonitoringService");

                if (existingProcesses.Length > 0)
                {
                    // A Worker is already running.
                    // Do NOT track it because this GOPark instance
                    // did not start it and therefore must not kill it.
                    return;
                }

                var baseDir =
                    AppDomain.CurrentDomain.BaseDirectory;

                const string exeName =
                    "ParkingMonitoringService.exe";

                string? targetPath = null;

                // -------------------------------------------------
                // 1. FINAL RELEASE VERSION
                // -------------------------------------------------
                //
                // GOPark\
                //   ParkingManagement.UI.exe
                //   Worker\
                //      ParkingMonitoringService.exe
                //
                // -------------------------------------------------

                var releaseWorkerPath =
                    Path.Combine(
                        baseDir,
                        "Worker",
                        exeName);

                if (File.Exists(releaseWorkerPath))
                {
                    targetPath =
                        releaseWorkerPath;
                }

                // -------------------------------------------------
                // 2. WORKER NEXT TO UI
                // -------------------------------------------------

                if (targetPath == null)
                {
                    var sameFolderPath =
                        Path.Combine(
                            baseDir,
                            exeName);

                    if (File.Exists(sameFolderPath))
                    {
                        targetPath =
                            sameFolderPath;
                    }
                }

                // -------------------------------------------------
                // 3. DEVELOPMENT:
                // Worker target = net8.0
                // -------------------------------------------------

                if (targetPath == null)
                {
                    var devPathNet8 =
                        Path.GetFullPath(
                            Path.Combine(
                                baseDir,
                                "..",
                                "..",
                                "..",
                                "..",
                                "ParkingMonitoringService",
                                "bin",
                                "Debug",
                                "net8.0",
                                exeName));

                    if (File.Exists(devPathNet8))
                    {
                        targetPath =
                            devPathNet8;
                    }
                }

                // -------------------------------------------------
                // 4. DEVELOPMENT:
                // Future Windows-specific Worker target
                // -------------------------------------------------

                if (targetPath == null)
                {
                    var devPathWindows =
                        Path.GetFullPath(
                            Path.Combine(
                                baseDir,
                                "..",
                                "..",
                                "..",
                                "..",
                                "ParkingMonitoringService",
                                "bin",
                                "Debug",
                                "net8.0-windows",
                                exeName));

                    if (File.Exists(devPathWindows))
                    {
                        targetPath =
                            devPathWindows;
                    }
                }

                // -------------------------------------------------
                // Worker not found
                // -------------------------------------------------

                if (targetPath == null)
                {
                    LogException(
                        new FileNotFoundException(
                            "ParkingMonitoringService.exe wurde nicht gefunden."),
                        "StartWorkerService");

                    return;
                }

                // -------------------------------------------------
                // Start Worker hidden
                // -------------------------------------------------

                var processInfo =
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = targetPath,

                        WorkingDirectory =
                            Path.GetDirectoryName(targetPath)
                            ?? baseDir,

                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                _workerProcess =
                    System.Diagnostics.Process.Start(
                        processInfo);
            }
            catch (Exception ex)
            {
                LogException(
                    ex,
                    "StartWorkerService");
            }
        }

        // =========================================================
        // APPLICATION EXIT
        // =========================================================

        protected override void OnExit(ExitEventArgs e)
        {
            // Kill only the Worker that THIS GOPark instance started.
            if (_workerProcess != null)
            {
                try
                {
                    if (!_workerProcess.HasExited)
                    {
                        _workerProcess.Kill();
                    }
                }
                catch (Exception ex)
                {
                    LogException(
                        ex,
                        "StopWorkerService");
                }
                finally
                {
                    _workerProcess.Dispose();
                    _workerProcess = null;
                }
            }

            base.OnExit(e);
        }
    }
}