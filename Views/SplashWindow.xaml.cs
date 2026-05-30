using System.Reflection;
using System.Windows;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            VersionText.Text = $"wersja {version?.Major}.{version?.Minor}.{version?.Build}";
        }

        public void SetStatus(string message)
        {
            Dispatcher.Invoke(() => StatusText.Text = message);
        }
    }
}
