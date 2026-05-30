using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using InwentaryzacjaSprzetu.ViewModels;
using InwentaryzacjaSprzetu.Models;
using InwentaryzacjaSprzetu.Services;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class EquipmentEditDialog : Window
    {
        private EquipmentEditViewModel? _viewModel;

        public EquipmentEditDialog()
        {
            InitializeComponent();
        }

        public EquipmentEditDialog(EquipmentEditViewModel viewModel) : this()
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

        private async void DepartmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is not Department selected || _viewModel == null) return;

            if (selected.Id == 0)
            {
                // Wybrano N/A — wyczyść dział, zostaw sentinel jako wybrany element
                _viewModel.SelectedDepartment = EquipmentEditViewModel.NoDepartmentSentinel;
                return;
            }

            if (selected.Id == -1)
            {
                // Pokaż dialog dodawania działu
                var result = MessageBox.Show(
                    "Czy chcesz wprowadzić własną nazwę działu?\n\nKliknij OK aby kontynuować, lub Anuluj aby wyjść.",
                    "Dodaj własny dział",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.OK)
                {
                    await OpenAddDepartmentDialogAsync();
                }
                
                // Resetuj ComboBox selection
                comboBox.SelectedIndex = -1;
            }
        }

        private async Task OpenAddDepartmentDialogAsync()
        {
            try
            {
                // Pobierz DepartmentService i ViewModel z App
                var departmentService = App.GetService<DepartmentService>();
                var departmentEditViewModel = App.GetService<DepartmentEditViewModel>();
                
                await departmentEditViewModel.InitializeAsync();
                
                var dialog = new DepartmentEditDialog(departmentEditViewModel);
                dialog.Owner = this;
                
                if (dialog.ShowDialog() == true)
                {
                    MessageBox.Show("Nowy dział został dodany pomyślnie!", "Sukces", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas dodawania działu: {ex.Message}", "Błąd", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
                _viewModel.SelectedEquipmentTemplate = null;
        }
    }
}