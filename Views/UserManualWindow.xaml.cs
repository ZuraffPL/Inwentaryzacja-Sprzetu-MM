using System.Windows;
using System.Windows.Controls;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class UserManualWindow : Window
    {
        public UserManualWindow()
        {
            InitializeComponent();
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string sectionName)
            {
                var section = FindName(sectionName) as FrameworkElement;
                if (section != null)
                    section.BringIntoView();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
