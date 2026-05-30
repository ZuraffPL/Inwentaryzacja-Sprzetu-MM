using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using InwentaryzacjaSprzetu.Helpers;
using InwentaryzacjaSprzetu.Models;
using InwentaryzacjaSprzetu.ViewModels;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class EquipmentView : UserControl
    {
        private bool _suppressTabChange = false;
        private bool _groupsExpanded = true;
        private AppPreferences _prefs = new();

        // Stan rozwinęcia per kategoria — aktualizowany przez zdarzenia Expander i GroupExpander_Loaded
        private readonly Dictionary<string, bool> _savedGroupState = new(StringComparer.OrdinalIgnoreCase);
        private bool _suppressGroupStateTracking = false;

        // Offset dla etykiet nagłówków grup — suma szerokości kolumn 0+1+2 minus overhead Expandera/Bordera
        public static readonly DependencyProperty BrandColumnOffsetProperty =
            DependencyProperty.Register(nameof(BrandColumnOffset), typeof(GridLength), typeof(EquipmentView),
                new PropertyMetadata(new GridLength(300)));

        public GridLength BrandColumnOffset
        {
            get => (GridLength)GetValue(BrandColumnOffsetProperty);
            private set => SetValue(BrandColumnOffsetProperty, value);
        }

        private void UpdateBrandColumnOffset()
        {
            if (EquipmentDataGrid.Columns.Count < 3) return;
            double col0 = EquipmentDataGrid.Columns[0].ActualWidth;
            double col1 = EquipmentDataGrid.Columns[1].ActualWidth;
            double col2 = EquipmentDataGrid.Columns[2].ActualWidth;
            if (col0 == 0 || col1 == 0 || col2 == 0) return;

            // Overhead: Border.Padding lewy (5px) + toggle Expandera (~19px)
            const double overhead = 24.0;
            double w = Math.Max(100.0, col0 + col1 + col2 - overhead);
            var newOffset = new GridLength(w);
            if (BrandColumnOffset.Value != newOffset.Value)
                BrandColumnOffset = newOffset;
        }

        // Polskie nazwy krajów do tooltipów i menu
        private static readonly Dictionary<string, string> CountryNames = new()
        {
            ["PL"] = "Polska",
            ["SK"] = "Słowacja",
            ["CZ"] = "Czechy",
            ["RO"] = "Rumunia",
            ["HU"] = "Węgry",
            ["HR"] = "Chorwacja",
        };

        public EquipmentView()
        {
            InitializeComponent();
            Loaded += EquipmentView_Loaded;
        }

        private void EquipmentView_Loaded(object sender, RoutedEventArgs e)
        {
            _prefs = AppPreferences.Load();
            _groupsExpanded = _prefs.EquipmentGroupsExpanded;
            UpdateToggleButtonContent();

            // Bąbelkowanie zdarzeń Expander z DataGrid — śledzenie ręcznych zmian stanu grup
            EquipmentDataGrid.AddHandler(Expander.ExpandedEvent, new RoutedEventHandler(OnGroupExpanded));
            EquipmentDataGrid.AddHandler(Expander.CollapsedEvent, new RoutedEventHandler(OnGroupCollapsed));

            // Dynamiczne obliczanie offsetu etykiet grup na podstawie faktycznych szerokości kolumn
            EquipmentDataGrid.LayoutUpdated += (_, _) => UpdateBrandColumnOffset();

            if (GetViewModel() is MainWindowViewModel vm)
            {
                RebuildCountryTabs(vm);
                RebuildPavilionTabs(vm);

                vm.Locations.CollectionChanged += (s, args) =>
                {
                    RebuildCountryTabs(vm);
                    RebuildPavilionTabs(vm);
                };

                // Podłącz filtr CollectionViewSource
                var cvs = (System.Windows.Data.CollectionViewSource)FindResource("EquipmentGrouped");
                cvs.Filter += EquipmentGrouped_Filter;

                vm.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(vm.SelectedLocationFilter))
                        SyncPavilionTabToViewModel(vm);

                    // Odśwież widok po zmianie rewizji filtra (EquipmentFilterRevision rośnie przy każdym tick)
                    if (args.PropertyName == nameof(vm.EquipmentFilterRevision))
                        cvs.View?.Refresh();
                };
            }
        }

        /// <summary>Callback filtrowania CollectionViewSource — multi-select po dziale, statusie i kategorii.</summary>
        private void EquipmentGrouped_Filter(object sender, System.Windows.Data.FilterEventArgs e)
        {
            if (e.Item is not Equipment eq) { e.Accepted = false; return; }
            var vm = GetViewModel();
            if (vm == null) { e.Accepted = true; return; }

            // Dział: jeśli żaden niezaznaczony → brak filtra (pokaż wszystkie)
            var activeDepts = vm.DepartmentFilterItems.Where(d => d.IsSelected).ToList();
            if (activeDepts.Count > 0 && !activeDepts.Any(d => eq.DepartmentId == d.Department?.Id))
            { e.Accepted = false; return; }

            // Status: jeśli żaden niezaznaczony → brak filtra (pokaż wszystkie)
            var activeStatuses = vm.StatusFilterItems.Where(s => s.IsSelected).ToList();
            if (activeStatuses.Count > 0 && !activeStatuses.Any(s => eq.Status == s.Status))
            { e.Accepted = false; return; }

            // Kategoria: jeśli żadna niezaznaczona → brak filtra (pokaż wszystkie)
            var activeCats = vm.CategoryFilterItems.Where(c => c.IsSelected).ToList();
            if (activeCats.Count > 0 && !activeCats.Any(c => eq.CategoryId == c.Category.Id))
            { e.Accepted = false; return; }

            e.Accepted = true;
        }

        // ===== Zakładki krajów =====

        private void RebuildCountryTabs(MainWindowViewModel vm)
        {
            _suppressTabChange = true;

            // Distinct kody krajów spośród lokalizacji, z wyłączeniem ukrytych
            var visibleCodes = vm.Locations
                .Where(l => !string.IsNullOrEmpty(l.CountryCode)
                         && !_prefs.HiddenCountryCodes.Contains(l.CountryCode!))
                .Select(l => l.CountryCode!)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            while (CountryTabControl.Items.Count > 1)
                CountryTabControl.Items.RemoveAt(1);

            foreach (var code in visibleCodes)
            {
                CountryTabControl.Items.Add(new TabItem
                {
                    Header = code,
                    Tag = code,
                    ToolTip = CountryNames.TryGetValue(code, out var name) ? name : code
                });
            }

            _suppressTabChange = false;
        }

        private void CountryTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressTabChange) return;
            var vm = GetViewModel();
            if (vm == null) return;

            // Zmiana kraju → przebuduj listę pawilonów i wyczyść filtr
            RebuildPavilionTabs(vm);
            vm.SelectedLocationFilter = null;
        }

        private void CountryMgmt_Click(object sender, RoutedEventArgs e)
        {
            var vm = GetViewModel();
            if (vm == null) return;

            var allCodes = vm.Locations
                .Where(l => !string.IsNullOrEmpty(l.CountryCode))
                .Select(l => l.CountryCode!)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            if (allCodes.Count == 0) return;

            var menu = new ContextMenu();
            menu.Items.Add(new MenuItem { Header = "Widoczność krajów:", IsEnabled = false,
                                          FontWeight = FontWeights.Bold });
            menu.Items.Add(new Separator());

            foreach (var code in allCodes)
            {
                var localCode = code;
                var displayName = CountryNames.TryGetValue(code, out var n) ? n : code;
                var item = new MenuItem
                {
                    Header = $"{code}  –  {displayName}",
                    IsCheckable = true,
                    IsChecked = !_prefs.HiddenCountryCodes.Contains(code),
                };
                item.Click += (s2, a2) =>
                {
                    // Przeładuj preferencje z dysku, aby nie nadpisać filtrów zapisanych przez ViewModel
                    var freshPrefs = AppPreferences.Load();
                    if (item.IsChecked)
                        freshPrefs.HiddenCountryCodes.Remove(localCode);
                    else
                        freshPrefs.HiddenCountryCodes.Add(localCode);
                    freshPrefs.Save();
                    _prefs = freshPrefs;
                    RebuildCountryTabs(vm);
                    RebuildPavilionTabs(vm);
                    vm.SelectedLocationFilter = null;
                };
                menu.Items.Add(item);
            }

            menu.PlacementTarget = CountryMgmtButton;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        // ===== Zakładki pawilonów =====

        private void RebuildPavilionTabs(MainWindowViewModel vm)
        {
            _suppressTabChange = true;

            var selectedCountry = (CountryTabControl.SelectedItem as TabItem)?.Tag as string;

            while (PavilionTabControl.Items.Count > 1)
                PavilionTabControl.Items.RemoveAt(1);

            // Pokaż tylko aktywne pawilony pasujące do wybranego kraju (lub wszystkie gdy kraj = null)
            var filtered = vm.Locations
                .Where(l => l.IsActive)
                .Where(l => selectedCountry == null || l.CountryCode == selectedCountry)
                .ToList();

            foreach (var loc in filtered)
            {
                PavilionTabControl.Items.Add(new TabItem
                {
                    Header = loc.Name,
                    Tag = loc,
                    ToolTip = loc.Code
                });
            }

            SyncPavilionTabToViewModel(vm);
            _suppressTabChange = false;
        }

        private void SyncPavilionTabToViewModel(MainWindowViewModel vm)
        {
            _suppressTabChange = true;

            if (vm.SelectedLocationFilter == null)
            {
                PavilionTabControl.SelectedIndex = 0;
            }
            else
            {
                foreach (TabItem item in PavilionTabControl.Items)
                {
                    if (item.Tag is Location loc && loc.Id == vm.SelectedLocationFilter.Id)
                    {
                        PavilionTabControl.SelectedItem = item;
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

            if (PavilionTabControl.SelectedItem is TabItem selected)
                vm.SelectedLocationFilter = selected.Tag as Location;
        }

        // ===== Toggle grup sprzętu =====

        /// <summary>
        /// Wywoływane przez XAML (Loaded event na Expander w GroupStyle ControlTemplate).
        /// Zapewnia poprawny stan IsExpanded przy każdym (re)tworzeniu grupy — zarówno
        /// przy pierwszym załadowaniu jak i po przeładowaniu danych lub wirtualizacji.
        /// </summary>
        private void GroupExpander_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Expander expander) return;
            var groupItem = FindAncestor<GroupItem>(expander);
            if (groupItem?.DataContext is not CollectionViewGroup grp || grp.Name is not string name) return;

            var vm = GetViewModel();
            bool expand;
            if (vm != null && vm.RequestExpandCategoryName == name)
            {
                // Po dodaniu/edycji sprzętu — wymuś rozwinięcie tej kategorii
                expand = true;
                vm.RequestExpandCategoryName = null;
            }
            else
            {
                expand = _savedGroupState.TryGetValue(name, out var saved) ? saved : _groupsExpanded;
            }

            _suppressGroupStateTracking = true;
            expander.IsExpanded = expand;
            _suppressGroupStateTracking = false;
            _savedGroupState[name] = expand;
        }

        private void OnGroupExpanded(object sender, RoutedEventArgs e)
        {
            if (_suppressGroupStateTracking) return;
            if (e.OriginalSource is Expander expander)
            {
                var groupItem = FindAncestor<GroupItem>(expander);
                if (groupItem?.DataContext is CollectionViewGroup grp && grp.Name is string name)
                    _savedGroupState[name] = true;
            }
        }

        private void OnGroupCollapsed(object sender, RoutedEventArgs e)
        {
            if (_suppressGroupStateTracking) return;
            if (e.OriginalSource is Expander expander)
            {
                var groupItem = FindAncestor<GroupItem>(expander);
                if (groupItem?.DataContext is CollectionViewGroup grp && grp.Name is string name)
                    _savedGroupState[name] = false;
            }
        }

        private void ToggleGroups_Click(object sender, RoutedEventArgs e)
        {
            _groupsExpanded = !_groupsExpanded;
            SetAllGroupsExpanded(_groupsExpanded);
            UpdateToggleButtonContent();
            // Przeładuj preferencje z dysku, aby nie nadpisać filtrów zapisanych przez ViewModel
            var prefs = AppPreferences.Load();
            prefs.EquipmentGroupsExpanded = _groupsExpanded;
            prefs.Save();
        }

        private void SetAllGroupsExpanded(bool expanded)
        {
            _suppressGroupStateTracking = true;
            EquipmentDataGrid.UpdateLayout();

            var groupItems = new List<GroupItem>();
            FindVisualChildren(EquipmentDataGrid, groupItems);
            foreach (var gi in groupItems)
            {
                if (gi.DataContext is CollectionViewGroup grp && grp.Name is string name)
                    _savedGroupState[name] = expanded;
                var exps = new List<Expander>();
                FindVisualChildren(gi, exps);
                foreach (var exp in exps)
                    exp.IsExpanded = expanded;
            }

            _suppressGroupStateTracking = false;
        }

        private void UpdateToggleButtonContent()
        {
            if (ToggleGroupsButton == null) return;
            ToggleGroupsButton.Content = _groupsExpanded ? "⊟" : "⊞";
            ToggleGroupsButton.ToolTip = _groupsExpanded
                ? "Zwiń wszystkie kategorie"
                : "Rozwiń wszystkie kategorie";
        }

        private static void FindVisualChildren<T>(DependencyObject parent, List<T> results) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) results.Add(t);
                FindVisualChildren(child, results);
            }
        }

        private static T? FindAncestor<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T t) return t;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        // ===== Interakcje DataGrid =====

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid && dataGrid.SelectedItem is Equipment selectedEquipment)
            {
                var mainViewModel = GetViewModel();
                if (mainViewModel?.EditEquipmentWithParameterCommand.CanExecute(selectedEquipment) == true)
                    mainViewModel.EditEquipmentWithParameterCommand.Execute(selectedEquipment);
            }
        }

        private void DataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dep = (DependencyObject)e.OriginalSource;
            while (dep != null && dep is not DataGridRow)
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is DataGridRow row)
            {
                row.IsSelected = true;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void CategoryAddEquipment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem) return;
            var contextMenu = menuItem.Parent as ContextMenu;
            var group = (contextMenu?.PlacementTarget as FrameworkElement)?.DataContext as System.Windows.Data.CollectionViewGroup;
            if (group == null) return;

            var categoryName = group.Name as string;
            var vm = GetViewModel();
            if (vm == null || string.IsNullOrEmpty(categoryName)) return;

            var category = vm.Categories.FirstOrDefault(c => c.Name == categoryName);
            if (category != null)
                vm.AddEquipmentForCategoryCommand.Execute(category);
        }

        private MainWindowViewModel? GetViewModel()
        {
            var window = Window.GetWindow(this);
            return window?.DataContext as MainWindowViewModel;
        }
    }
}
