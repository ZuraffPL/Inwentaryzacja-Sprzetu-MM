using System;
using System.Windows;
using InwentaryzacjaSprzetu.ViewModels;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class CategoryEditDialog : Window
    {
        private CategoryEditViewModel? _viewModel;

        public CategoryEditDialog()
        {
            InitializeComponent();
        }

        public CategoryEditDialog(CategoryEditViewModel viewModel) : this()
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