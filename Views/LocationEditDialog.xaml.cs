using System;
using System.Windows;
using InwentaryzacjaSprzetu.ViewModels;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class LocationEditDialog : Window
    {
        private LocationEditViewModel? _viewModel;

        public LocationEditDialog()
        {
            InitializeComponent();
        }

        public LocationEditDialog(LocationEditViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = viewModel;
            
            // Podłącz event dla zamknięcia okna po zapisaniu
            viewModel.OnSaveCompleted += () =>
            {
                DialogResult = true;
                Close();
            };
        }
    }
}