using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using InwentaryzacjaSprzetu.ViewModels;
using InwentaryzacjaSprzetu.Models;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class LocationsView : UserControl
    {
        public LocationsView()
        {
            InitializeComponent();
        }

        private MainWindowViewModel? GetViewModel()
            => Window.GetWindow(this)?.DataContext as MainWindowViewModel;

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is Location loc)
            {
                var vm = GetViewModel();
                if (vm?.EditLocationCommand.CanExecute(loc) == true)
                    vm.EditLocationCommand.Execute(loc);
            }
        }

        /// <summary>Zaznacza kliknięty wiersz przy PPM, blokuje klik na pustym miejscu.</summary>
        private void DataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dep = (DependencyObject)e.OriginalSource;
            while (dep != null && dep is not DataGridRow)
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is DataGridRow row)
                row.IsSelected = true;
            else
                e.Handled = true;
        }

        /// <summary>Przy otwieraniu menu kontekstowego ustawia DataContext na zaznaczoną lokalizację.</summary>
        private void LocationContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu)
                menu.DataContext = LocationsDataGrid.SelectedItem as Location;
        }

        private void MenuItemEdit_Click(object sender, RoutedEventArgs e)
        {
            var loc = LocationsDataGrid.SelectedItem as Location;
            var vm = GetViewModel();
            if (loc != null && vm?.EditLocationWithParameterCommand.CanExecute(loc) == true)
                vm.EditLocationWithParameterCommand.Execute(loc);
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            var loc = LocationsDataGrid.SelectedItem as Location;
            var vm = GetViewModel();
            if (loc != null && vm?.DeleteLocationWithParameterCommand.CanExecute(loc) == true)
                vm.DeleteLocationWithParameterCommand.Execute(loc);
        }

        /// <summary>Auto-zapis po zaznaczeniu/odznaczeniu checkboxa "Aktywna" bezpośrednio w siatce.</summary>
        private async void LocationsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (e.Row.Item is not Location loc) return;

            await Dispatcher.InvokeAsync(async () =>
            {
                var vm = GetViewModel();
                if (vm != null)
                    await vm.SaveLocationIsActiveAsync(loc);
            });
        }
    }
}