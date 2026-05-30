using System.Windows;
using InwentaryzacjaSprzetu.ViewModels;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class EventEditDialog : Window
    {
        private EventEditViewModel? _viewModel;

        public EventEditDialog()
        {
            InitializeComponent();
        }

        public EventEditDialog(EventEditViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = viewModel;
            
            // Podłącz event dla zamknięcia okna po zapisaniu
            viewModel.OnSaveCompleted += () =>
            {
                DialogResult = true;
                Close();
            };
            
            // Podłącz event dla anulowania
            viewModel.OnCancelRequested += () =>
            {
                DialogResult = false;
                Close();
            };
        }
    }
}