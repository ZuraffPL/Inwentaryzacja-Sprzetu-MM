using System.Reflection;
using System.Windows;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            VersionLabel.Text = $"{version?.Major}.{version?.Minor}.{version?.Build}";
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
