using System.Windows;
using InwentaryzacjaSprzetu.ViewModels;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class DepartmentEditDialog : Window
    {
        public DepartmentEditViewModel ViewModel { get; }

        public DepartmentEditDialog(DepartmentEditViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsValid())
            {
                MessageBox.Show("Proszę wypełnić wszystkie wymagane pola (kod i nazwa).", 
                               "Błąd walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var success = await ViewModel.SaveAsync();
            if (success)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Wystąpił błąd podczas zapisywania działu.", 
                               "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}