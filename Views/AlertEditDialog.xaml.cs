using System.Windows;
using InwentaryzacjaSprzetu.ViewModels;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class AlertEditDialog : Window
    {
        public AlertEditDialog()
        {
            InitializeComponent();
        }

        public AlertEditDialog(AlertEditViewModel viewModel) : this()
        {
            DataContext = viewModel;

            viewModel.OnSaveCompleted   += () => { DialogResult = true;  Close(); };
            viewModel.OnCancelRequested += () => { DialogResult = false; Close(); };
        }
    }
}
