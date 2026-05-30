using System.Windows.Controls;
using System.Windows.Input;
using InwentaryzacjaSprzetu.ViewModels;
using InwentaryzacjaSprzetu.Models;

namespace InwentaryzacjaSprzetu.Views
{
    /// <summary>
    /// Interaction logic for CategoriesView.xaml
    /// </summary>
    public partial class CategoriesView : UserControl
    {
        public CategoriesView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Sprawdź czy kliknięto na rekord, a nie na nagłówek
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is Category selectedCategory)
            {
                // Znajdź MainWindowViewModel z DataContext okna nadrzędnego
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow?.DataContext is MainWindowViewModel mainViewModel)
                {
                    // Wywołaj komendę edycji kategorii
                    if (mainViewModel.EditCategoryCommand.CanExecute(selectedCategory))
                    {
                        mainViewModel.EditCategoryCommand.Execute(selectedCategory);
                    }
                }
            }
        }
    }
}