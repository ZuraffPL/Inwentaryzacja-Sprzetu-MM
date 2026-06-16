using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using InwentaryzacjaSprzetu.ViewModels;
using InwentaryzacjaSprzetu.Models;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class EventsView : UserControl
    {
        private bool _suppressTabChange = false;
        private bool _activeGroupsExpanded = true;
        private bool _archivedGroupsExpanded = false;

        public EventsView()
        {
            InitializeComponent();
            Loaded += EventsView_Loaded;
        }

        private void EventsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (GetViewModel() is MainWindowViewModel vm)
            {
                RebuildLocationTabs(vm);

                // Przebuduj zakładki gdy lista lokalizacji się zmieni
                vm.Locations.CollectionChanged += (s, args) => RebuildLocationTabs(vm);

                // Synchronizuj zakładkę gdy filtr zmieni się z zewnątrz
                vm.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(vm.SelectedEventLocationFilter))
                        SyncTabToViewModel(vm);
                };
            }
        }

        private void RebuildLocationTabs(MainWindowViewModel vm)
        {
            var tabs = EventsPavilionTabControl;
            if (tabs == null) return;

            _suppressTabChange = true;

            // Zachowaj zakładkę "Wszystkie" (index 0), usuń resztę
            while (tabs.Items.Count > 1)
                tabs.Items.RemoveAt(1);

            foreach (var location in vm.Locations.Where(l => l.IsActive))
            {
                tabs.Items.Add(new TabItem
                {
                    Header = location.Name,
                    Tag = location
                });
            }

            SyncTabToViewModel(vm);
            _suppressTabChange = false;
        }

        private void SyncTabToViewModel(MainWindowViewModel vm)
        {
            var tabs = EventsPavilionTabControl;
            if (tabs == null) return;

            _suppressTabChange = true;

            if (vm.SelectedEventLocationFilter == null)
            {
                tabs.SelectedIndex = 0;
            }
            else
            {
                foreach (TabItem item in tabs.Items)
                {
                    if (item.Tag is Location loc && loc.Id == vm.SelectedEventLocationFilter.Id)
                    {
                        tabs.SelectedItem = item;
                        break;
                    }
                }
            }

            _suppressTabChange = false;
        }

        private void PavilionTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressTabChange) return;

            var vm = GetViewModel();
            if (vm == null) return;

            if (sender is TabControl tabs && tabs.SelectedItem is TabItem selected)
                vm.SelectedEventLocationFilter = selected.Tag as Location;
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is InventoryEvent selectedEvent)
            {
                var vm = GetViewModel();
                if (vm?.EditEventWithParameterCommand.CanExecute(selectedEvent) == true)
                    vm.EditEventWithParameterCommand.Execute(selectedEvent);
            }
        }

        private void ToggleActiveGroups_Click(object sender, RoutedEventArgs e)
        {
            _activeGroupsExpanded = !_activeGroupsExpanded;
            SetAllGroupsExpanded(ActiveEventsDataGrid, _activeGroupsExpanded);
            UpdateToggleButton(ToggleActiveGroupsButton, _activeGroupsExpanded);
        }

        private void ToggleArchivedGroups_Click(object sender, RoutedEventArgs e)
        {
            _archivedGroupsExpanded = !_archivedGroupsExpanded;
            SetAllGroupsExpanded(ArchivedEventsDataGrid, _archivedGroupsExpanded);
            UpdateToggleButton(ToggleArchivedGroupsButton, _archivedGroupsExpanded);
        }

        private void SetAllGroupsExpanded(DataGrid dataGrid, bool expanded)
        {
            dataGrid.UpdateLayout();
            var groupItems = new System.Collections.Generic.List<GroupItem>();
            FindVisualChildren(dataGrid, groupItems);
            foreach (var gi in groupItems)
            {
                var expanders = new System.Collections.Generic.List<Expander>();
                FindVisualChildren(gi, expanders);
                foreach (var exp in expanders)
                    exp.IsExpanded = expanded;
            }
        }

        private static void UpdateToggleButton(Button? btn, bool expanded)
        {
            if (btn == null) return;
            btn.Content = expanded ? "⊟" : "⊞";
            btn.ToolTip = expanded ? "Zwiń wszystkie grupy" : "Rozwiń wszystkie grupy";
        }

        private static void FindVisualChildren<T>(DependencyObject parent, System.Collections.Generic.List<T> results) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) results.Add(t);
                FindVisualChildren(child, results);
            }
        }

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

        private void ActiveEventsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var vm = GetViewModel();
            if (vm == null) return;
            var selected = vm.SelectedEvent;

            // Ctrl+Z — Dodaj załącznik do zaznaczonego zdarzenia
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                if (selected != null && vm.AddAttachmentToEventWithParameterCommand.CanExecute(selected))
                    vm.AddAttachmentToEventWithParameterCommand.Execute(selected);
                return;
            }

            // Z — Pokaż załączniki
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true;
                if (selected != null && vm.ShowEventAttachmentsWithParameterCommand.CanExecute(selected))
                    vm.ShowEventAttachmentsWithParameterCommand.Execute(selected);
                return;
            }

            if (e.Key != Key.A) return;

            // Zawsze konsumuj klawisz A w DataGrid — zapobiega bąbelkowaniu do Expandera grupy
            e.Handled = true;

            // Archiwizuj zaznaczone zdarzenie klawiszem A
            if (selected != null && selected.EventStatus == EventStatus.Active
                && vm.ArchiveEventWithParameterCommand.CanExecute(selected))
            {
                vm.ArchiveEventWithParameterCommand.Execute(selected);
            }
        }

        private void ArchivedEventsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var vm = GetViewModel();
            if (vm == null) return;
            var selected = vm.SelectedEvent;

            // Ctrl+Z — Dodaj załącznik
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                if (selected != null && vm.AddAttachmentToEventWithParameterCommand.CanExecute(selected))
                    vm.AddAttachmentToEventWithParameterCommand.Execute(selected);
                return;
            }

            // Z — Pokaż załączniki
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true;
                if (selected != null && vm.ShowEventAttachmentsWithParameterCommand.CanExecute(selected))
                    vm.ShowEventAttachmentsWithParameterCommand.Execute(selected);
            }
        }

        private MainWindowViewModel? GetViewModel()
        {
            var window = Window.GetWindow(this);
            return window?.DataContext as MainWindowViewModel;
        }

        // ===== ZWIJANIE / ROZWIJANIE SEKCJI =====

        private bool _activeSectionExpanded = true;
        private bool _archivedSectionExpanded = true;

        private void ActiveSectionHeader_Click(object sender, MouseButtonEventArgs e)
        {
            _activeSectionExpanded = !_activeSectionExpanded;
            ActiveEventsDataGrid.Visibility = _activeSectionExpanded ? Visibility.Visible : Visibility.Collapsed;
            ActiveDataGridRow.Height = _activeSectionExpanded ? new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) : new System.Windows.GridLength(0);
            ActiveSectionCollapseIcon.Text = _activeSectionExpanded ? "▼" : "►";
        }

        private void ArchivedSectionHeader_Click(object sender, MouseButtonEventArgs e)
        {
            _archivedSectionExpanded = !_archivedSectionExpanded;
            ArchivedEventsDataGrid.Visibility = _archivedSectionExpanded ? Visibility.Visible : Visibility.Collapsed;
            ArchivedDataGridRow.Height = _archivedSectionExpanded ? new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) : new System.Windows.GridLength(0);
            ArchivedSectionCollapseIcon.Text = _archivedSectionExpanded ? "▼" : "▶";
        }
    }
}
