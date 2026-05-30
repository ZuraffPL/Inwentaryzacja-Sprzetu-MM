using System.Windows;
using System.Windows.Input;
using System.Reflection;
using InwentaryzacjaSprzetu.ViewModels;

namespace InwentaryzacjaSprzetu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            // Dodanie wersji do tytułu
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Title = $"Inwentaryzacja Sprz\u0119tu - v{version?.Major}.{version?.Minor}.{version?.Build}";
            
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void UserManualMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new Views.UserManualWindow() { Owner = this };
            window.ShowDialog();
        }

        private void ChangelogMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var prefs = InwentaryzacjaSprzetu.Helpers.AppPreferences.Load();
            var window = new Views.ChangelogWindow(prefs) { Owner = this };
            window.ShowDialog();
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new Views.AboutWindow() { Owner = this };
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