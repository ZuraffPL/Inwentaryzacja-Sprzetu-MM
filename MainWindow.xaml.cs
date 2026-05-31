using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Reflection;
using InwentaryzacjaSprzetu.Helpers;
using InwentaryzacjaSprzetu.ViewModels;
using InwentaryzacjaSprzetu.Views;

namespace InwentaryzacjaSprzetu
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private System.Windows.Forms.NotifyIcon _notifyIcon = null!;

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Title = $"Inwentaryzacja Sprzętu - v{version?.Major}.{version?.Minor}.{version?.Build}";
            
            InitializeNotifyIcon();
            
            // Nasłuchuj zmiany liczby alertów — aktualizuj tooltip i ikonę traya
            _viewModel.AlertCountChanged += count =>
            {
                if (count > 0)
                {
                    _notifyIcon.Text = $"Inwentaryzacja Sprzętu — {count} aktywnych powiadomień";
                    _notifyIcon.Icon = TrayIconHelper.CreateBellAlertIcon();
                }
                else
                {
                    _notifyIcon.Text = "Inwentaryzacja Sprzętu";
                    _notifyIcon.Icon = TrayIconHelper.CreateBellIcon();
                }
            };

            Loaded += MainWindow_Loaded;
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Text    = "Inwentaryzacja Sprzętu",
                Icon    = TrayIconHelper.CreateBellIcon(),
                Visible = true
            };

            // Menu kontekstowe traya
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("Pokaż okno", null, (_, _) => RestoreWindow());
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add("Wyjście", null, (_, _) => Application.Current.Shutdown());
            _notifyIcon.ContextMenuStrip = contextMenu;

            // Podwójne kliknięcie → przywróć okno
            _notifyIcon.DoubleClick += (_, _) => RestoreWindow();
        }

        private void RestoreWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        protected override void OnStateChanged(System.EventArgs e)
        {
            base.OnStateChanged(e);
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                _notifyIcon.ShowBalloonTip(
                    2000,
                    "Inwentaryzacja Sprzętu",
                    "Aplikacja działa w tle. Kliknij dwukrotnie ikonę traya, aby przywrócić.",
                    System.Windows.Forms.ToolTipIcon.Info);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _notifyIcon.Dispose();
            base.OnClosing(e);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();
            ShowStartupAlertsIfNeeded();
        }

        /// <summary>
        /// Po załadowaniu danych sprawdza czy są wyzwolone alerty.
        /// Jeśli tak — i użytkownik nie odznaczył „nie pokazuj dziś" — otwiera okno startowe.
        /// </summary>
        private void ShowStartupAlertsIfNeeded()
        {
            var prefs        = AppPreferences.Load();
            var todayKey     = System.DateTime.Today.ToString("yyyy-MM-dd");
            var dismissedToday = prefs.AlertStartupDismissedDate == todayKey;

            if (dismissedToday) return;

            var triggered = _viewModel.ActiveAlerts.Where(a => a.IsTriggered).ToList();
            if (triggered.Count == 0) return;

            var window = new AlertsStartupWindow(triggered, prefs) { Owner = this };
            window.ShowDialog();

            if (window.NavigateToAlerts)
                _viewModel.ShowAlertsViewCommand.Execute(null);
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void UserManualMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new UserManualWindow() { Owner = this };
            window.ShowDialog();
        }

        private void ChangelogMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var prefs = AppPreferences.Load();
            var window = new ChangelogWindow(prefs) { Owner = this };
            window.ShowDialog();
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new AboutWindow() { Owner = this };
            window.ShowDialog();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel.SearchCommand.Execute(null);
            }
        }
    }
}