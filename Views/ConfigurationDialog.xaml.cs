using System.Windows;
using InwentaryzacjaSprzetu.Helpers;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class ConfigurationDialog : Window
    {
        private readonly AppPreferences _prefs;

        public bool RunOnStartup
        {
            get => _prefs.RunOnStartup;
            set => _prefs.RunOnStartup = value;
        }

        public ConfigurationDialog(AppPreferences prefs)
        {
            InitializeComponent();
            _prefs = prefs;

            // Odczytaj rzeczywisty stan z rejestru (źródło prawdy)
            _prefs.RunOnStartup = AutostartHelper.IsEnabled();
            DataContext = this;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            AutostartHelper.SetEnabled(_prefs.RunOnStartup);
            _prefs.Save();
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
