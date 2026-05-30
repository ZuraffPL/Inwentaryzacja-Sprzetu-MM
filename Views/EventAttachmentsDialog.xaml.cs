using System.Windows;
using InwentaryzacjaSprzetu.ViewModels;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class EventAttachmentsDialog : Window
    {
        public EventAttachmentsDialog(EventAttachmentsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
