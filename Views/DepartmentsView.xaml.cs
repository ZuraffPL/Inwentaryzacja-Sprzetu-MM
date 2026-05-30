using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class DepartmentsView : UserControl
    {
        public DepartmentsView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem != null)
            {
                var editCommand = DataContext?.GetType().GetProperty("EditDepartmentCommand")?.GetValue(DataContext) as ICommand;
                editCommand?.Execute(null);
            }
        }
    }
}