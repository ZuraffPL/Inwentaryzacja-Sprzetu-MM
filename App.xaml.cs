using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using InwentaryzacjaSprzetu.Database;
using InwentaryzacjaSprzetu.Services;
using InwentaryzacjaSprzetu.ViewModels;
using InwentaryzacjaSprzetu.Views;
using Microsoft.EntityFrameworkCore;

namespace InwentaryzacjaSprzetu
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost _host;

        public App()
        {
            // Ustaw polską lokalizację dla całej aplikacji
            SetPolishCulture();
            _host = CreateHostBuilder().Build();

            // Globalna obsługa nieoczekiwanych wyjątków
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private void SetPolishCulture()
        {
            // Ustawienie polskiej kultury (formatowanie dat, liczb, walut)
            var culture = new CultureInfo("pl-PL");
            
            // Ustaw dla UI
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            
            // Ustaw dla wszystkich nowych wątków w aplikacji
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            
            // Ustaw format dat i liczb dla WPF
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            var splash = new SplashWindow();
            splash.Show();

            // Odliczanie minimum 5s startuje równocześnie z inicjalizacją
            var minDisplayTask = Task.Delay(5000);

            try
            {
                splash.SetStatus("Uruchamianie hosta aplikacji...");
                await _host.StartAsync();

                using (var scope = _host.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                    var loggingService = scope.ServiceProvider.GetRequiredService<ILoggingService>();

                    try
                    {
                        splash.SetStatus("Inicjalizacja bazy danych...");
                        await loggingService.LogInfoAsync("Rozpoczynanie migracji bazy danych...");
                        await context.Database.MigrateAsync();
                        await loggingService.LogInfoAsync("Migracja bazy danych zakończona pomyślnie");

                        splash.SetStatus("Wczytywanie danych początkowych...");
                        await loggingService.LogInfoAsync("Dodawanie danych początkowych...");
                        await context.SeedInitialDataAsync();
                        await loggingService.LogInfoAsync("Dane początkowe dodane.");

                        splash.SetStatus("Naprawa historycznych danych zdarzeń...");
                        var eventService = scope.ServiceProvider.GetRequiredService<IInventoryEventService>();
                        await eventService.RepairHistoricalEventsAsync();
                        await loggingService.LogInfoAsync("Naprawa historycznych zdarzeń zakończona.");
                    }
                    catch (Exception ex)
                    {
                        await loggingService.LogErrorAsync("Błąd podczas migracji bazy danych", ex);
                        throw;
                    }
                }

                splash.SetStatus("Otwieranie okna głównego...");
                var mainWindow = _host.Services.GetRequiredService<MainWindow>();

                // Poczekaj na upłynięcie minimum 3 sekund (jeśli startup był szybszy)
                await minDisplayTask;

                splash.Close();

                // Jawnie ustaw MainWindow — SplashWindow był pierwszym oknem i stał się
                // Application.MainWindow automatycznie; musisz to naprawić przed Show()
                this.MainWindow = mainWindow;
                mainWindow.Show();

                // Pokaż changelog jeśli nowa wersja (lub użytkownik nie wyłączył auto-show)
                var prefs = Helpers.AppPreferences.Load();
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                var versionString = $"{version?.Major}.{version?.Minor}.{version?.Build}";
                if (prefs.ChangelogDismissedVersion != versionString)
                {
                    var changelog = new Views.ChangelogWindow(prefs) { Owner = mainWindow };
                    changelog.ShowDialog();
                }

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                splash.Close();
                MessageBox.Show($"Błąd podczas uruchamiania aplikacji:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync();
            }

            base.OnExit(e);
        }

        private static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Baza danych - folder AppData chroni bazę przed nadpisaniem przy aktualizacji aplikacji
                    services.AddDbContext<InventoryDbContext>(options =>
                    {
                        var appDataPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "InwentaryzacjaSprzetu");
                        Directory.CreateDirectory(appDataPath);
                        var dbPath = Path.Combine(appDataPath, "inventory.db");
                        options.UseSqlite($"Data Source={dbPath}");
                    });

                    // Serwisy
                    services.AddSingleton<ILoggingService, LoggingService>();
                    services.AddScoped<IEquipmentService, EquipmentService>();
                    services.AddScoped<ICategoryService, CategoryService>();
                    services.AddScoped<ILocationService, LocationService>();
                    services.AddScoped<IDepartmentService, DepartmentService>();
                    services.AddScoped<IInventoryEventService, InventoryEventService>();
                    services.AddScoped<IEventAttachmentService, EventAttachmentService>();
                    services.AddScoped<IAlertService, AlertService>();

                    // ViewModels
                    services.AddTransient<MainWindowViewModel>();

                    // Dialog ViewModels
                    services.AddTransient<EquipmentEditViewModel>();
                    services.AddTransient<CategoryEditViewModel>();
                    services.AddTransient<LocationEditViewModel>();
                    services.AddTransient<DepartmentEditViewModel>();
                    services.AddTransient<EventEditViewModel>();
                    services.AddTransient<EventAttachmentsViewModel>();
                    services.AddTransient<AlertEditViewModel>();

                    // Windows
                    services.AddSingleton<MainWindow>();
                });
        }

        public static T GetService<T>() where T : class
        {
            return ((App)Current)._host.Services.GetRequiredService<T>();
        }

        /// <summary>Bezwzględna ścieżka do pliku bazy danych w AppData\Local\InwentaryzacjaSprzetu</summary>
        public static string DatabasePath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InwentaryzacjaSprzetu",
            "inventory.db");

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogUnhandledException("Nieobsłużony wyjątek UI (Dispatcher)", e.Exception);
            e.Handled = true;
            MessageBox.Show(
                $"Wystąpił nieoczekiwany błąd:\n{e.Exception.Message}\n\nSzczegóły zostały zapisane do debug_log.txt",
                "Błąd aplikacji", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogUnhandledException("Nieobsłużony wyjątek domeny aplikacji", e.ExceptionObject as Exception);
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogUnhandledException("Nieobserwowany wyjątek Task", e.Exception);
            e.SetObserved();
        }

        private void LogUnhandledException(string context, Exception? ex)
        {
            try
            {
                var logger = _host.Services.GetService<ILoggingService>();
                logger?.LogErrorAsync(context, ex).GetAwaiter().GetResult();
            }
            catch { }
        }
    }
}