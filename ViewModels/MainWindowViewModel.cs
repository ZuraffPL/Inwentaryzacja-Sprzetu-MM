using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.IO.Compression;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwentaryzacjaSprzetu.Database;
using InwentaryzacjaSprzetu.Helpers;
using InwentaryzacjaSprzetu.Models;
using InwentaryzacjaSprzetu.Services;
using InwentaryzacjaSprzetu.Views;

namespace InwentaryzacjaSprzetu.ViewModels
{
    /// <summary>Klikalny element filtra działu (multi-select).</summary>
    public sealed partial class SelectableDepartmentFilter : ObservableObject
    {
        public Department? Department { get; init; }
        public string Label => Department?.Name ?? "— brak działu —";

        [ObservableProperty]
        private bool _isSelected;
    }

    /// <summary>Klikalny element filtra statusu sprzętu (multi-select).</summary>
    public sealed partial class SelectableStatusFilter : ObservableObject
    {
        public EquipmentStatus Status { get; init; }
        public string Label { get; init; } = "";

        [ObservableProperty]
        private bool _isSelected;
    }

    /// <summary>Klikalny element filtra kategorii sprzętu (multi-select).</summary>
    public sealed partial class SelectableCategoryFilter : ObservableObject
    {
        public Category Category { get; init; } = null!;
        public string Label => Category.Name;

        [ObservableProperty]
        private bool _isSelected;
    }

    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IEquipmentService _equipmentService;
        private readonly ICategoryService _categoryService;
        private readonly ILocationService _locationService;
        private readonly IDepartmentService _departmentService;
        private readonly IInventoryEventService _eventService;
        private readonly IServiceProvider _serviceProvider;
        private AppPreferences _prefs = AppPreferences.Load();

        [ObservableProperty]
        private string _currentView = "Equipment";

        [ObservableProperty]
        private string _searchText = string.Empty;

        partial void OnSearchTextChanged(string value)
        {
            // Automatyczne wyszukiwanie po 500ms od ostatniej zmiany
            _ = Task.Run(async () =>
            {
                await Task.Delay(500);
                if (value == SearchText) // Sprawdź czy tekst się nie zmienił w międzyczasie
                {
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await SearchAsync();
                    });
                }
            });
        }

        [ObservableProperty]
        private string _statusMessage = "Gotowy";

        [ObservableProperty]
        private Equipment? _selectedEquipment;

        [ObservableProperty]
        private Category? _selectedCategory;

        [ObservableProperty]
        private Location? _selectedLocation;

        private Location? _selectedLocationFilter;
        public Location? SelectedLocationFilter
        {
            get => _selectedLocationFilter;
            set
            {
                if (SetProperty(ref _selectedLocationFilter, value))
                {
                    // Automatycznie przeładuj sprzęt po zmianie filtra
                    _ = LoadEquipmentAsync();
                }
            }
        }

        private Location? _selectedEventLocationFilter;
        public Location? SelectedEventLocationFilter
        {
            get => _selectedEventLocationFilter;
            set
            {
                if (SetProperty(ref _selectedEventLocationFilter, value))
                {
                    // Automatycznie przeładuj zdarzenia po zmianie filtra
                    _ = LoadEventsAsync();
                }
            }
        }

        // ===== Filtry widoku sprzętu (multi-select) =====

        /// <summary>Lista klikalnych filtrów działu — przebudowywana przy ładowaniu działów.</summary>
        public ObservableCollection<SelectableDepartmentFilter> DepartmentFilterItems { get; } = new();

        /// <summary>Lista klikalnych filtrów kategorii — przebudowywana przy ładowaniu kategorii.</summary>
        public ObservableCollection<SelectableCategoryFilter> CategoryFilterItems { get; } = new();

        /// <summary>Lista filtrów statusu — stała, inicjalizowana na starcie.</summary>
        public ObservableCollection<SelectableStatusFilter> StatusFilterItems { get; } = new(new[]
        {
            new SelectableStatusFilter { Status = EquipmentStatus.Active,           Label = "Sprawny"                 },
            new SelectableStatusFilter { Status = EquipmentStatus.Inactive,         Label = "Rezerwa"                 },
            new SelectableStatusFilter { Status = EquipmentStatus.UnderMaintenance, Label = "W konserwacji"           },
            new SelectableStatusFilter { Status = EquipmentStatus.Damaged,          Label = "Zepsuty (uszkodzony)"   },
            new SelectableStatusFilter { Status = EquipmentStatus.Disposed,         Label = "Zutylizowany (kasacja)" },
        });

        /// <summary>Wersja filtra — rośnie przy każdej zmianie IsSelected na dowolnym elemencie. Kod-behind nasłuchuje i odświeża widok.</summary>
        [ObservableProperty]
        private int _equipmentFilterRevision;

        public string DepartmentFilterSummary
        {
            get
            {
                var count = DepartmentFilterItems.Count(d => d.IsSelected);
                return count == 0 ? "Wszystkie działy" : $"Działów: {count}";
            }
        }

        public string CategoryFilterSummary
        {
            get
            {
                var count = CategoryFilterItems.Count(c => c.IsSelected);
                return count == 0 ? "Wszystkie kategorie" : $"Kategorii: {count}";
            }
        }

        public string StatusFilterSummary
        {
            get
            {
                var count = StatusFilterItems.Count(s => s.IsSelected);
                return count == 0 ? "Wszystkie statusy" : $"Statusów: {count}";
            }
        }

        private void OnFilterItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectableDepartmentFilter.IsSelected) ||
                e.PropertyName == nameof(SelectableStatusFilter.IsSelected) ||
                e.PropertyName == nameof(SelectableCategoryFilter.IsSelected))
            {
                EquipmentFilterRevision++;
                OnPropertyChanged(nameof(DepartmentFilterSummary));
                OnPropertyChanged(nameof(StatusFilterSummary));
                OnPropertyChanged(nameof(CategoryFilterSummary));

                // Persystuj stan filtrów w preferencjach użytkownika
                _prefs.EquipmentFilterDepartmentCodes = DepartmentFilterItems
                    .Where(d => d.IsSelected && d.Department?.Code != null)
                    .Select(d => d.Department!.Code)
                    .ToList();
                _prefs.EquipmentFilterStatusValues = StatusFilterItems
                    .Where(s => s.IsSelected)
                    .Select(s => (int)s.Status)
                    .ToList();
                _prefs.EquipmentFilterCategoryCodes = CategoryFilterItems
                    .Where(c => c.IsSelected)
                    .Select(c => c.Category.Code)
                    .Where(code => code != null)
                    .Select(code => code!)
                    .ToList();
                _prefs.Save();
            }
        }

        [RelayCommand]
        private void ClearEquipmentFilters()
        {
            foreach (var item in DepartmentFilterItems) item.IsSelected = false;
            foreach (var item in StatusFilterItems) item.IsSelected = false;
            foreach (var item in CategoryFilterItems) item.IsSelected = false;
        }

        /// <summary>
        /// Eksportuje aktualnie przefiltrowany widok sprzętu do pliku PDF.
        /// Filtrowanie (dział, status) odwzorowane zgodnie z aktywnymi filtrami.
        /// Lokalizacja (pawilon) pochodzi z SelectedLocationFilter.
        /// </summary>
        [RelayCommand]
        private void ExportEquipmentToPdf()
        {
            try
            {
                // Zastosuj te same filtry co CollectionViewSource
                var activeDepts    = DepartmentFilterItems.Where(d => d.IsSelected).ToList();
                var activeStatuses = StatusFilterItems.Where(s => s.IsSelected).ToList();
                var activeCats     = CategoryFilterItems.Where(c => c.IsSelected).ToList();

                IEnumerable<Models.Equipment> source = Equipment;

                if (activeDepts.Count > 0)
                    source = source.Where(e => activeDepts.Any(d => e.DepartmentId == d.Department?.Id));

                if (activeStatuses.Count > 0)
                    source = source.Where(e => activeStatuses.Any(s => e.Status == s.Status));

                if (activeCats.Count > 0)
                    source = source.Where(e => activeCats.Any(c => e.CategoryId == c.Category.Id));

                var exportList = source.ToList();

                if (exportList.Count == 0)
                {
                    MessageBox.Show("Brak pozycji sprzętu do eksportu dla wybranych filtrów.",
                                    "Eksport PDF", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Tytuł — lokalizacja z filtra lub lista unikalnych lokalizacji z eksportowanych danych
                string locationPart;
                if (SelectedLocationFilter != null)
                {
                    locationPart = $" – {SelectedLocationFilter.Name}";
                }
                else
                {
                    var uniqueLocations = exportList
                        .Where(e => e.Location != null)
                        .Select(e => e.Location!.Name)
                        .Distinct()
                        .OrderBy(n => n)
                        .ToList();
                    locationPart = uniqueLocations.Count > 0
                        ? " – " + string.Join(", ", uniqueLocations)
                        : "";
                }
                var title = $"Inwentaryzacja sprzętu{locationPart}";

                var filterParts = new System.Collections.Generic.List<string>();
                if (activeDepts.Count > 0)
                    filterParts.Add("Działy: " + string.Join(", ", activeDepts.Select(d => d.Label)));
                if (activeStatuses.Count > 0)
                    filterParts.Add("Statusy: " + string.Join(", ", activeStatuses.Select(s => s.Label)));
                if (activeCats.Count > 0)
                    filterParts.Add("Kategorie: " + string.Join(", ", activeCats.Select(c => c.Label)));

                string? filterDesc = filterParts.Count > 0 ? string.Join(" | ", filterParts) : null;

                // SaveFileDialog
                var defaultName = $"inwentaryzacja_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                var dialog = new SaveFileDialog
                {
                    Title            = "Zapisz raport PDF",
                    Filter           = "Plik PDF (*.pdf)|*.pdf",
                    FileName         = defaultName,
                    DefaultExt       = ".pdf",
                    AddExtension     = true,
                };

                if (dialog.ShowDialog() != true) return;

                EquipmentPdfExportService.Export(
                    dialog.FileName,
                    exportList,
                    title,
                    filterDesc,
                    hideDepartmentColumn: activeDepts.Count > 0,
                    hideStatusColumn: activeStatuses.Count > 0);

                StatusMessage = $"Eksport PDF zapisany: {System.IO.Path.GetFileName(dialog.FileName)}";

                // Zapytaj czy otworzyć plik
                var open = MessageBox.Show(
                    $"Plik PDF zapisany pomyślnie.\n{dialog.FileName}\n\nCzy otworzyć plik?",
                    "Eksport PDF", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (open == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd eksportu PDF: {ex.Message}";
                MessageBox.Show($"Nie udało się wygenerować pliku PDF:\n\n{ex.Message}",
                                "Błąd eksportu PDF", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================================================

        public ObservableCollection<Equipment> Equipment { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<Location> Locations { get; } = new();
        public ObservableCollection<Department> Departments { get; } = new();
        public ObservableCollection<InventoryEvent> ActiveEvents { get; } = new();
        public ObservableCollection<InventoryEvent> ArchivedEvents { get; } = new();

        // Liczniki sprzętu w status barze (przeliczane przy każdej zmianie kolekcji)
        public int ReserveDepartmentCount   => Equipment.Count(e => e.Department?.Code == "REZ");
        public int StatusActiveCount        => Equipment.Count(e => e.Status == Models.EquipmentStatus.Active);
        public int StatusInactiveCount      => Equipment.Count(e => e.Status == Models.EquipmentStatus.Inactive);
        public int StatusMaintenanceCount   => Equipment.Count(e => e.Status == Models.EquipmentStatus.UnderMaintenance);
        public int StatusDamagedCount       => Equipment.Count(e => e.Status == Models.EquipmentStatus.Damaged);
        public int StatusDisposedCount      => Equipment.Count(e => e.Status == Models.EquipmentStatus.Disposed);

        /// <summary>Po przeładowaniu sprzętu EquipmentView upewni się, że ta kategoria jest rozwinięta.</summary>
        public string? RequestExpandCategoryName { get; set; }

        // Dynamiczne nagłówki kolumn DataGrid
        // K=Komputery: System/RAM/Procesor, M=Monitory: Marka/Model/Przekątna, F=Kasy Fiskalne: Marka/Model/Nr unikatowy
        public string BrandColumnHeader        => SelectedCategory?.Code == "K" ? "System"      : "Marka";
        public string ModelColumnHeader        => SelectedCategory?.Code == "K" ? "RAM"         : "Model";
        public string SerialNumberColumnHeader => SelectedCategory?.Code switch
        {
            "K" => "Procesor",
            "M" => "Przekątna",
            "F" => "Nr unikatowy",
            _   => "Nr Seryjny"
        };

        partial void OnSelectedCategoryChanged(Category? value)
        {
            OnPropertyChanged(nameof(BrandColumnHeader));
            OnPropertyChanged(nameof(ModelColumnHeader));
            OnPropertyChanged(nameof(SerialNumberColumnHeader));
        }

        [ObservableProperty]
        private Department? _selectedDepartment;

        [ObservableProperty]
        private InventoryEvent? _selectedEvent;

        public MainWindowViewModel(
            IEquipmentService equipmentService,
            ICategoryService categoryService,
            ILocationService locationService,
            IDepartmentService departmentService,
            IInventoryEventService eventService,
            IServiceProvider serviceProvider)
        {
            _equipmentService = equipmentService;
            _categoryService = categoryService;
            _locationService = locationService;
            _departmentService = departmentService;
            _eventService = eventService;
            _serviceProvider = serviceProvider;

            Equipment.CollectionChanged += (_, _) => NotifyEquipmentCountsChanged();

            // Przywróć zapisany stan filtra statusu
            foreach (var item in StatusFilterItems)
            {
                item.IsSelected = _prefs.EquipmentFilterStatusValues.Contains((int)item.Status);
                item.PropertyChanged += OnFilterItemPropertyChanged;
            }
        }

        private void NotifyEquipmentCountsChanged()
        {
            OnPropertyChanged(nameof(ReserveDepartmentCount));
            OnPropertyChanged(nameof(StatusActiveCount));
            OnPropertyChanged(nameof(StatusInactiveCount));
            OnPropertyChanged(nameof(StatusMaintenanceCount));
            OnPropertyChanged(nameof(StatusDamagedCount));
            OnPropertyChanged(nameof(StatusDisposedCount));
        }

        public async Task InitializeAsync()
        {
            StatusMessage = "Ładowanie danych...";
            
            try
            {
                await LoadEquipmentAsync();
                await LoadCategoriesAsync();
                await LoadLocationsAsync();
                await LoadDepartmentsAsync();
                await LoadEventsAsync();

                // Jeśli przywrócono filtry statusu (ustawione w konstruktorze przed ładowaniem danych),
                // wymusz odświeżenie widoku — Equipment był ładowany zanim CVS filtr miał szansę zadziałać
                if (_prefs.EquipmentFilterStatusValues.Count > 0)
                    EquipmentFilterRevision++;

                StatusMessage = "Gotowy";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas ładowania danych: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task LoadEquipmentAsync()
        {
            try
            {
                // Filtruj według wybranego pawilonu bezpośrednio w bazie (izolacja danych per pawilon)
                var equipment = SelectedLocationFilter != null
                    ? await _equipmentService.GetByLocationAsync(SelectedLocationFilter.Id)
                    : await _equipmentService.GetAllAsync();

                Equipment.Clear();
                foreach (var item in equipment)
                {
                    Equipment.Add(item);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas ładowania sprzętu: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ClearLocationFilter()
        {
            SelectedLocationFilter = null;
        }

        [RelayCommand]
        private async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = await _categoryService.GetAllAsync();
                Categories.Clear();
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }

                // Przebuduj elementy filtra kategorii z subskrypcjami
                foreach (var item in CategoryFilterItems)
                    item.PropertyChanged -= OnFilterItemPropertyChanged;

                CategoryFilterItems.Clear();
                bool anyCatRestored = false;
                foreach (var cat in categories.OrderBy(c => c.SortOrder))
                {
                    var item = new SelectableCategoryFilter { Category = cat };
                    bool restored = cat.Code != null && _prefs.EquipmentFilterCategoryCodes.Contains(cat.Code);
                    item.IsSelected = restored;
                    if (restored) anyCatRestored = true;
                    item.PropertyChanged += OnFilterItemPropertyChanged;
                    CategoryFilterItems.Add(item);
                }

                OnPropertyChanged(nameof(CategoryFilterSummary));

                if (anyCatRestored)
                    EquipmentFilterRevision++;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas ładowania kategorii: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task LoadLocationsAsync()
        {
            try
            {
                var locations = await _locationService.GetAllIncludingInactiveAsync();
                Locations.Clear();
                foreach (var location in locations)
                {
                    Locations.Add(location);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas ładowania lokalizacji: {ex.Message}";
            }
        }

        /// <summary>Zapisuje zmianę IsActive lokalizacji (np. po kliknięciu checkboxa w siatce) i odświeża kolekcję.</summary>
        public async Task SaveLocationIsActiveAsync(Location location)
        {
            try
            {
                await _locationService.UpdateAsync(location);
                await LoadLocationsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd zapisu lokalizacji: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task LoadDepartmentsAsync()
        {
            try
            {
                var departments = await _departmentService.GetAllAsync();
                Departments.Clear();
                foreach (var department in departments)
                {
                    Departments.Add(department);
                }

                // Przebuduj elementy filtra działu z subskrypcjami
                foreach (var item in DepartmentFilterItems)
                    item.PropertyChanged -= OnFilterItemPropertyChanged;

                DepartmentFilterItems.Clear();
                bool anyDeptRestored = false;
                foreach (var dept in departments.Where(d => d.IsActive))
                {
                    var item = new SelectableDepartmentFilter { Department = dept };
                    // Przywróć zapisany stan filtra działu (porównanie po kodzie działu)
                    bool restored = dept.Code != null && _prefs.EquipmentFilterDepartmentCodes.Contains(dept.Code);
                    item.IsSelected = restored;
                    if (restored) anyDeptRestored = true;
                    item.PropertyChanged += OnFilterItemPropertyChanged;
                    DepartmentFilterItems.Add(item);
                }

                OnPropertyChanged(nameof(DepartmentFilterSummary));

                // Jeśli przywrócono jakiekolwiek filtry działów, wymusz odświeżenie widoku
                if (anyDeptRestored)
                    EquipmentFilterRevision++;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas ładowania działów: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task LoadEventsAsync()
        {
            try
            {
                var events = SelectedEventLocationFilter != null
                    ? await _eventService.GetByLocationAsync(SelectedEventLocationFilter.Id)
                    : await _eventService.GetAllAsync();

                ActiveEvents.Clear();
                ArchivedEvents.Clear();

                foreach (var eventItem in events)
                {
                    if (eventItem.EventStatus == Models.EventStatus.Completed)
                        ArchivedEvents.Add(eventItem);
                    else
                        ActiveEvents.Add(eventItem);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas ładowania zdarzeń: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await RefreshCurrentViewAsync();
                return;
            }

            try
            {
                StatusMessage = "Wyszukiwanie...";
                
                switch (CurrentView)
                {
                    case "Equipment":
                        await SearchEquipmentAsync();
                        break;
                    case "Categories":
                        await SearchCategoriesAsync();
                        break;
                    case "Locations":
                        await SearchLocationsAsync();
                        break;
                    case "Events":
                        await SearchEventsAsync();
                        break;
                    default:
                        await SearchEquipmentAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas wyszukiwania: {ex.Message}";
            }
        }

        private async Task SearchEquipmentAsync()
        {
            var results = await _equipmentService.SearchAsync(SearchText);
            Equipment.Clear();
            foreach (var item in results)
            {
                Equipment.Add(item);
            }
            StatusMessage = $"Znaleziono {Equipment.Count} rekordów sprzętu dla zapytania: '{SearchText}'";
        }

        private async Task SearchCategoriesAsync()
        {
            var results = await _categoryService.SearchAsync(SearchText);
            Categories.Clear();
            foreach (var item in results)
            {
                Categories.Add(item);
            }
            StatusMessage = $"Znaleziono {Categories.Count} rekordów kategorii dla zapytania: '{SearchText}'";
        }

        private async Task SearchLocationsAsync()
        {
            var results = await _locationService.SearchAllAsync(SearchText);
            Locations.Clear();
            foreach (var item in results)
            {
                Locations.Add(item);
            }
            StatusMessage = $"Znaleziono {Locations.Count} rekordów lokalizacji dla zapytania: '{SearchText}'";
        }

        private async Task SearchEventsAsync()
        {
            var results = await _eventService.SearchAsync(SearchText);
            ActiveEvents.Clear();
            ArchivedEvents.Clear();
            foreach (var item in results)
            {
                if (item.EventStatus == Models.EventStatus.Completed)
                    ArchivedEvents.Add(item);
                else
                    ActiveEvents.Add(item);
            }
            StatusMessage = $"Znaleziono {ActiveEvents.Count + ArchivedEvents.Count} rekordów zdarzeń dla zapytania: '{SearchText}'";
        }

        private async Task RefreshCurrentViewAsync()
        {
            switch (CurrentView)
            {
                case "Equipment":
                    await LoadEquipmentAsync();
                    StatusMessage = $"Wyświetlono wszystkie rekordy sprzętu ({Equipment.Count})";
                    break;
                case "Categories":
                    await LoadCategoriesAsync();
                    StatusMessage = $"Wyświetlono wszystkie rekordy kategorii ({Categories.Count})";
                    break;
                case "Locations":
                    await LoadLocationsAsync();
                    StatusMessage = $"Wyświetlono wszystkie rekordy lokalizacji ({Locations.Count})";
                    break;
                case "Events":
                    await LoadEventsAsync();
                    StatusMessage = $"Wyświetlono zdarzenia: {ActiveEvents.Count} aktywnych, {ArchivedEvents.Count} w archiwum";
                    break;
                default:
                    await LoadEquipmentAsync();
                    StatusMessage = $"Wyświetlono wszystkie rekordy sprzętu ({Equipment.Count})";
                    break;
            }
        }

        [RelayCommand]
        private async Task ClearSearchAsync()
        {
            SearchText = string.Empty;
            await RefreshCurrentViewAsync();
        }

        [RelayCommand]
        private void ShowEquipmentView()
        {
            CurrentView = "Equipment";
        }

        [RelayCommand]
        private void ShowCategoriesView()
        {
            CurrentView = "Categories";
        }

        [RelayCommand]
        private void ShowLocationsView()
        {
            CurrentView = "Locations";
        }

        [RelayCommand]
        private void ShowEventsView()
        {
            CurrentView = "Events";
        }

        // ===== CRUD Commands for Equipment =====
        [RelayCommand]
        private async Task AddEquipmentAsync()
        {
            try
            {
                var viewModel = _serviceProvider.GetRequiredService<EquipmentEditViewModel>();
                await viewModel.InitializeAsync();
                
                var dialog = new EquipmentEditDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    RequestExpandCategoryName = viewModel.SelectedCategory?.Name;
                    await LoadEquipmentAsync();
                    StatusMessage = "Dodano nowy sprzęt";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas dodawania sprzętu: {ex.Message}";
            }
        }

        /// <summary>Otwiera dialog dodawania sprzętu z pre-ustawioną kategorią (wywoływane z PPM na nagłówku grupy).</summary>
        [RelayCommand]
        private async Task AddEquipmentForCategoryAsync(Category? category)
        {
            if (category == null) return;
            try
            {
                var viewModel = _serviceProvider.GetRequiredService<EquipmentEditViewModel>();
                await viewModel.InitializeForCategoryAsync(category, SelectedLocationFilter);

                var dialog = new EquipmentEditDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;

                if (dialog.ShowDialog() == true)
                {
                    RequestExpandCategoryName = category.Name;
                    await LoadEquipmentAsync();
                    StatusMessage = $"Dodano nowy sprzęt do kategorii: {category.Name}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas dodawania sprzętu: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task EditEquipmentAsync()
        {
            if (SelectedEquipment == null)
            {
                MessageBox.Show("Wybierz sprzęt do edycji.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var viewModel = _serviceProvider.GetRequiredService<EquipmentEditViewModel>();
                await viewModel.InitializeAsync(SelectedEquipment);
                
                var dialog = new EquipmentEditDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    RequestExpandCategoryName = viewModel.SelectedCategory?.Name;
                    await LoadEquipmentAsync();
                    StatusMessage = "Zaktualizowano sprzęt";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas edycji sprzętu: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteEquipmentAsync()
        {
            if (SelectedEquipment == null)
            {
                MessageBox.Show("Wybierz sprzęt do usunięcia.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć sprzęt '{SelectedEquipment.Name}'?", 
                "Potwierdzenie usunięcia", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _equipmentService.DeleteAsync(SelectedEquipment.Id);
                    await LoadEquipmentAsync();
                    StatusMessage = "Usunięto sprzęt";
                    SelectedEquipment = null;
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Błąd podczas usuwania sprzętu: {ex.Message}";
                }
            }
        }

        // ===== Equipment Commands with Parameters (for Context Menu) =====
        [RelayCommand]
        private async Task EditEquipmentWithParameterAsync(Equipment? equipment)
        {
            if (equipment == null)
            {
                MessageBox.Show("Nie wybrano sprzętu do edycji.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var viewModel = _serviceProvider.GetRequiredService<EquipmentEditViewModel>();
                await viewModel.InitializeAsync(equipment);
                
                var dialog = new EquipmentEditDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    RequestExpandCategoryName = viewModel.SelectedCategory?.Name;
                    await LoadEquipmentAsync();
                    StatusMessage = "Zaktualizowano sprzęt";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas edycji sprzętu: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteEquipmentWithParameterAsync(Equipment? equipment)
        {
            if (equipment == null)
            {
                MessageBox.Show("Nie wybrano sprzętu do usunięcia.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć sprzęt: {equipment.Name}?",
                "Potwierdzenie usunięcia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _equipmentService.DeleteAsync(equipment.Id);
                    await LoadEquipmentAsync();
                    StatusMessage = "Usunięto sprzęt";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Błąd podczas usuwania sprzętu: {ex.Message}";
                }
            }
        }

        // ===== CRUD Commands for Categories =====
        [RelayCommand]
        private async Task AddCategoryAsync()
        {
            try
            {
                var viewModel = _serviceProvider.GetRequiredService<CategoryEditViewModel>();
                await viewModel.InitializeAsync();
                
                var dialog = new CategoryEditDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    await LoadCategoriesAsync();
                    StatusMessage = "Dodano nową kategorię";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas dodawania kategorii: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task EditCategoryAsync()
        {
            if (SelectedCategory == null)
            {
                MessageBox.Show("Wybierz kategorię do edycji.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var viewModel = _serviceProvider.GetRequiredService<CategoryEditViewModel>();
                await viewModel.InitializeAsync(SelectedCategory);
                
                var dialog = new CategoryEditDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    await LoadCategoriesAsync();
                    StatusMessage = "Zaktualizowano kategorię";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas edycji kategorii: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteCategoryAsync()
        {
            if (SelectedCategory == null)
            {
                MessageBox.Show("Wybierz kategorię do usunięcia.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć kategorię '{SelectedCategory.Name}'?\n\nUwaga: Może to wpłynąć na sprzęt przypisany do tej kategorii.", 
                "Potwierdzenie usunięcia", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _categoryService.DeleteAsync(SelectedCategory.Id);
                    await LoadCategoriesAsync();
                    StatusMessage = "Usunięto kategorię";
                    SelectedCategory = null;
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Błąd podczas usuwania kategorii: {ex.Message}";
                }
            }
        }

        // ===== Category Commands with Parameters (for Context Menu) =====
        [RelayCommand]
        private async Task EditCategoryWithParameterAsync(Category? category)
        {
            if (category == null)
            {
                MessageBox.Show("Nie wybrano kategorii do edycji.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var viewModel = _serviceProvider.GetRequiredService<CategoryEditViewModel>();
                await viewModel.InitializeAsync(category);
                
                var dialog = new CategoryEditDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    await LoadCategoriesAsync();
                    StatusMessage = "Zaktualizowano kategorię";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas edycji kategorii: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteCategoryWithParameterAsync(Category? category)
        {
            if (category == null)
            {
                MessageBox.Show("Nie wybrano kategorii do usunięcia.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć kategorię: {category.Name}?",
                "Potwierdzenie usunięcia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _categoryService.DeleteAsync(category.Id);
                    await LoadCategoriesAsync();
                    StatusMessage = "Usunięto kategorię";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Błąd podczas usuwania kategorii: {ex.Message}";
                }
            }
        }

        // ===== CRUD Commands for Locations =====
        [RelayCommand]
        private async Task AddLocationAsync()
        {
            try
            {
                var loggingService = _serviceProvider.GetRequiredService<ILoggingService>();
                await loggingService.LogInfoAsync("MainWindowViewModel.AddLocationAsync() - START");
                
                var viewModel = _serviceProvider.GetRequiredService<LocationEditViewModel>();
                await loggingService.LogInfoAsync("MainWindowViewModel.AddLocationAsync() - LocationEditViewModel created");
                
                await viewModel.InitializeAsync();
                await loggingService.LogInfoAsync("MainWindowViewModel.AddLocationAsync() - ViewModel initialized");
                
                var dialog = new LocationEditDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;
                await loggingService.LogInfoAsync("MainWindowViewModel.AddLocationAsync() - Dialog created, showing...");
                
                if (dialog.ShowDialog() == true)
                {
                    await loggingService.LogInfoAsync("MainWindowViewModel.AddLocationAsync() - Dialog result: true, reloading locations");
                    await LoadLocationsAsync();
                    StatusMessage = "Dodano nową lokalizację";
                    await loggingService.LogInfoAsync("MainWindowViewModel.AddLocationAsync() - SUCCESS");
                }
                else
                {
                    await loggingService.LogInfoAsync("MainWindowViewModel.AddLocationAsync() - Dialog result: false/null, operation cancelled");
                }
            }
            catch (Exception ex)
            {
                var loggingService = _serviceProvider.GetRequiredService<ILoggingService>();
                await loggingService.LogErrorAsync("MainWindowViewModel.AddLocationAsync() - Error occurred", ex);
                StatusMessage = $"Błąd podczas dodawania lokalizacji: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task EditLocationAsync()
        {
            if (SelectedLocation == null)
            {
                MessageBox.Show("Wybierz lokalizację do edycji.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var viewModel = _serviceProvider.GetRequiredService<LocationEditViewModel>();
                await viewModel.InitializeAsync(SelectedLocation);
                
                var dialog = new LocationEditDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    await LoadLocationsAsync();
                    StatusMessage = "Zaktualizowano lokalizację";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas edycji lokalizacji: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteLocationAsync()
        {
            if (SelectedLocation == null)
            {
                MessageBox.Show("Wybierz lokalizację do usunięcia.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć lokalizację '{SelectedLocation.Name}'?\n\nUwaga: Może to wpłynąć na sprzęt przypisany do tej lokalizacji.", 
                "Potwierdzenie usunięcia", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _locationService.DeleteAsync(SelectedLocation.Id);
                    await LoadLocationsAsync();
                    StatusMessage = "Usunięto lokalizację";
                    SelectedLocation = null;
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Błąd podczas usuwania lokalizacji: {ex.Message}";
                }
            }
        }

        // ===== Location Commands with Parameters (for Context Menu) =====
        [RelayCommand]
        private async Task EditLocationWithParameterAsync(Location? location)
        {
            if (location == null)
            {
                MessageBox.Show("Nie wybrano lokalizacji do edycji.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var viewModel = _serviceProvider.GetRequiredService<LocationEditViewModel>();
                await viewModel.InitializeAsync(location);
                
                var dialog = new LocationEditDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    await LoadLocationsAsync();
                    StatusMessage = "Zaktualizowano lokalizację";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas edycji lokalizacji: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteLocationWithParameterAsync(Location? location)
        {
            if (location == null)
            {
                MessageBox.Show("Nie wybrano lokalizacji do usunięcia.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć lokalizację: {location.Name}?",
                "Potwierdzenie usunięcia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _locationService.DeleteAsync(location.Id);
                    await LoadLocationsAsync();
                    StatusMessage = "Usunięto lokalizację";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Błąd podczas usuwania lokalizacji: {ex.Message}";
                }
            }
        }

        // ===== EVENTS MANAGEMENT =====

        [RelayCommand]
        private async Task AddEventAsync()
        {
            try
            {
                var viewModel = _serviceProvider.GetRequiredService<EventEditViewModel>();
                await viewModel.InitializeAsync();
                
                var dialog = new EventEditDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    await LoadEventsAsync();
                    StatusMessage = "Dodano nowe zdarzenie";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas dodawania zdarzenia: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task AddEventForEquipmentAsync(Equipment? equipment)
        {
            if (equipment == null)
            {
                MessageBox.Show("Nie wybrano sprzętu dla zdarzenia.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var viewModel = _serviceProvider.GetRequiredService<EventEditViewModel>();
                // Inicjalizacja z pre-populate danych wybranego sprzętu
                await viewModel.InitializeForEquipmentAsync(equipment);

                var dialog = new EventEditDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;

                if (dialog.ShowDialog() == true)
                {
                    await LoadEventsAsync();
                    // Odśwież listę sprzętu — zdarzenie mogło zmienić status lub cenę zakupu
                    await LoadEquipmentAsync();
                    StatusMessage = $"Dodano zdarzenie dla sprzętu: {equipment.Name}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas dodawania zdarzenia: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task EditEventAsync()
        {
            if (SelectedEvent == null)
            {
                MessageBox.Show("Wybierz zdarzenie do edycji.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var viewModel = _serviceProvider.GetRequiredService<EventEditViewModel>();
                await viewModel.InitializeAsync(SelectedEvent);
                
                var dialog = new EventEditDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    await LoadEventsAsync();
                    // Odśwież sprzęt — zdarzenie mogło zmienić status lub cenę zakupu
                    await LoadEquipmentAsync();
                    StatusMessage = "Zaktualizowano zdarzenie";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas edycji zdarzenia: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteEventAsync()
        {
            if (SelectedEvent == null)
            {
                MessageBox.Show("Wybierz zdarzenie do usunięcia.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć zdarzenie '{SelectedEvent.Description}'?", 
                "Potwierdzenie usunięcia", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _eventService.DeleteAsync(SelectedEvent.Id);
                    await LoadEventsAsync();
                    StatusMessage = "Usunięto zdarzenie";
                    SelectedEvent = null;
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Błąd podczas usuwania zdarzenia: {ex.Message}";
                }
            }
        }

        // ===== Event Commands with Parameters (for Context Menu) =====
        [RelayCommand]
        private async Task EditEventWithParameterAsync(InventoryEvent? eventItem)
        {
            if (eventItem == null)
            {
                MessageBox.Show("Nie wybrano zdarzenia do edycji.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var viewModel = _serviceProvider.GetRequiredService<EventEditViewModel>();
                await viewModel.InitializeAsync(eventItem);
                
                var dialog = new EventEditDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    await LoadEventsAsync();
                    // Odśwież sprzęt — zdarzenie mogło zmienić status lub cenę zakupu
                    await LoadEquipmentAsync();
                    StatusMessage = "Zaktualizowano zdarzenie";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas edycji zdarzenia: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteEventWithParameterAsync(InventoryEvent? eventItem)
        {
            if (eventItem == null)
            {
                MessageBox.Show("Nie wybrano zdarzenia do usunięcia.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć zdarzenie: {eventItem.EventType} z {eventItem.EventDate:dd.MM.yyyy}?",
                "Potwierdzenie usunięcia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _eventService.DeleteAsync(eventItem.Id);
                    await LoadEventsAsync();
                    StatusMessage = "Usunięto zdarzenie";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Błąd podczas usuwania zdarzenia: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        private async Task ArchiveEventWithParameterAsync(InventoryEvent? eventItem)
        {
            if (eventItem == null) return;
            try
            {
                eventItem.EventStatus = Models.EventStatus.Completed;
                await _eventService.UpdateAsync(eventItem);
                await LoadEventsAsync();
                StatusMessage = "Zdarzenie przeniesione do archiwum";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas archiwizacji zdarzenia: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task RestoreEventWithParameterAsync(InventoryEvent? eventItem)
        {
            if (eventItem == null) return;
            try
            {
                eventItem.EventStatus = Models.EventStatus.Active;
                await _eventService.UpdateAsync(eventItem);
                await LoadEventsAsync();
                StatusMessage = "Zdarzenie przywrócone z archiwum";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas przywracania zdarzenia: {ex.Message}";
            }
        }

        // ===== Załączniki zdarzeń =====

        [RelayCommand]
        private async Task AddAttachmentToEventWithParameterAsync(InventoryEvent? eventItem)
        {
            if (eventItem == null) return;
            var attachmentService = _serviceProvider.GetRequiredService<IEventAttachmentService>();

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title  = "Wybierz plik załącznika",
                Filter = "Obsługiwane pliki (*.jpg;*.jpeg;*.png;*.webp;*.pdf)|*.jpg;*.jpeg;*.png;*.webp;*.pdf" +
                         "|Wszystkie pliki (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                var result = await attachmentService.AddAsync(eventItem.Id, dialog.FileName);
                if (result != null)
                {
                    StatusMessage = $"Dodano załącznik: {result.OriginalFileName}";
                    MessageBox.Show($"Załącznik '{result.OriginalFileName}' został pomyślnie dodany.",
                        "Załącznik dodany", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd dodawania załącznika: {ex.Message}";
                MessageBox.Show($"Nie udało się dodać załącznika:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ShowEventAttachmentsWithParameterAsync(InventoryEvent? eventItem)
        {
            if (eventItem == null) return;
            try
            {
                var viewModel = _serviceProvider.GetRequiredService<EventAttachmentsViewModel>();
                await viewModel.InitializeAsync(eventItem);
                var dialog = new Views.EventAttachmentsDialog(viewModel);
                dialog.Owner = System.Windows.Application.Current.MainWindow;
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd otwierania załączników: {ex.Message}";
                MessageBox.Show($"Nie udało się otworzyć okna załączników:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ShowEquipmentEventsWithParameter(Equipment? equipment)
        {
            if (equipment == null) return;
            var window = new Views.EquipmentEventsWindow(equipment, _eventService);
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.Show();
        }

        [RelayCommand(CanExecute = nameof(CanShowConnectedEquipment))]
        private void ShowConnectedEquipmentWithParameter(Equipment? equipment)
        {
            if (equipment == null) return;
            var window = new Views.ConnectedEquipmentWindow(equipment, _equipmentService);
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.Show();
        }

        private static bool CanShowConnectedEquipment(Equipment? equipment)
            => equipment?.Category?.Code is "K" or "S";

        // ===== DEPARTMENTS MANAGEMENT =====
        [RelayCommand]
        private async Task AddDepartmentAsync()
        {
            try
            {
                var viewModel = _serviceProvider.GetRequiredService<DepartmentEditViewModel>();
                await viewModel.InitializeAsync();
                
                var dialog = new DepartmentEditDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    await LoadDepartmentsAsync();
                    StatusMessage = "Dodano nowy dział";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas dodawania działu: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task EditDepartmentAsync()
        {
            if (SelectedDepartment == null)
            {
                MessageBox.Show("Nie wybrano działu do edycji.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var viewModel = _serviceProvider.GetRequiredService<DepartmentEditViewModel>();
                await viewModel.InitializeAsync(SelectedDepartment);
                
                var dialog = new DepartmentEditDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    await LoadDepartmentsAsync();
                    StatusMessage = "Zaktualizowano dział";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas edycji działu: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteDepartmentAsync()
        {
            if (SelectedDepartment == null)
            {
                MessageBox.Show("Nie wybrano działu do usunięcia.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć dział: {SelectedDepartment.Name}?",
                "Potwierdzenie usunięcia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _departmentService.DeleteAsync(SelectedDepartment.Id);
                    await LoadDepartmentsAsync();
                    StatusMessage = "Usunięto dział";
                    SelectedDepartment = null;
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Błąd podczas usuwania działu: {ex.Message}";
                }
            }
        }

        // ===== Department Commands with Parameters (for Context Menu) =====
        [RelayCommand]
        private async Task EditDepartmentWithParameterAsync(Department? department)
        {
            if (department == null)
            {
                MessageBox.Show("Nie wybrano działu do edycji.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var viewModel = _serviceProvider.GetRequiredService<DepartmentEditViewModel>();
                await viewModel.InitializeAsync(department);
                
                var dialog = new DepartmentEditDialog(viewModel);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true)
                {
                    await LoadDepartmentsAsync();
                    StatusMessage = "Zaktualizowano dział";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd podczas edycji działu: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DeleteDepartmentWithParameterAsync(Department? department)
        {
            if (department == null)
            {
                MessageBox.Show("Nie wybrano działu do usunięcia.", "Informacja", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć dział: {department.Name}?",
                "Potwierdzenie usunięcia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _departmentService.DeleteAsync(department.Id);
                    await LoadDepartmentsAsync();
                    StatusMessage = "Usunięto dział";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Błąd podczas usuwania działu: {ex.Message}";
                }
            }
        }

        // ===== TOOLS: EXPORT / IMPORT DATABASE =====

        [RelayCommand]
        private async Task ExportDatabase()
        {
            try
            {
                var dbPath = App.DatabasePath;
                if (!File.Exists(dbPath))
                {
                    MessageBox.Show("Nie znaleziono pliku bazy danych.", "Błąd",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Title = "Eksportuj bazę danych",
                    Filter = "Baza danych SQLite (*.db)|*.db|Wszystkie pliki (*.*)|*.*",
                    FileName = $"inventory_export_{DateTime.Now:yyyyMMdd_HHmm}.db",
                    DefaultExt = ".db"
                };

                if (dialog.ShowDialog() != true)
                    return;

                StatusMessage = "Eksportowanie bazy danych...";

                // VACUUM INTO tworzy czystą, spójną kopię bazy (checkpoint WAL + kompakcja).
                // Działa przy otwartych połączeniach — SQLite gwarantuje spójność snapshotu.
                await using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = "VACUUM INTO $dest";
                cmd.Parameters.AddWithValue("$dest", dialog.FileName);
                await cmd.ExecuteNonQueryAsync();

                StatusMessage = $"Baza danych wyeksportowana do: {Path.GetFileName(dialog.FileName)}";
                MessageBox.Show(
                    $"Baza danych została wyeksportowana pomyślnie.\n\nPlik: {dialog.FileName}",
                    "Eksport zakończony", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd eksportu: {ex.Message}";
                MessageBox.Show($"Błąd podczas eksportu bazy danych:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ImportDatabaseAsync()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Importuj bazę danych",
                    Filter = "Baza danych SQLite (*.db)|*.db|Wszystkie pliki (*.*)|*.*",
                    DefaultExt = ".db"
                };

                if (dialog.ShowDialog() != true)
                    return;

                // Weryfikacja — plik musi zaczynać się od magic bytes SQLite
                if (!IsValidSqliteFile(dialog.FileName))
                {
                    MessageBox.Show(
                        "Wybrany plik nie jest poprawną bazą danych SQLite.\nSprawdź czy plik nie jest uszkodzony.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Weryfikacja integralności importowanego pliku
                StatusMessage = "Sprawdzanie integralności pliku...";
                var integrityError = CheckSqliteIntegrity(dialog.FileName);
                if (integrityError != null)
                {
                    MessageBox.Show(
                        $"Wybrany plik bazy danych jest uszkodzony i nie może być zaimportowany.\n\nSzczegół: {integrityError}",
                        "Błąd integralności", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var confirm = MessageBox.Show(
                    $"Czy na pewno chcesz zaimportować bazę danych z pliku:\n{dialog.FileName}?\n\n"
                    + "Aktualna baza zostanie zastąpiona.\nKopia zapasowa zostanie zapisana automatycznie.",
                    "Potwierdź import",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                    return;

                StatusMessage = "Importowanie bazy danych...";

                var dbPath = App.DatabasePath;
                string? backupPath = null;

                // 1. Kopia zapasowa aktualnej bazy przed nadpisaniem
                if (File.Exists(dbPath))
                {
                    backupPath = dbPath + $".backup_{DateTime.Now:yyyyMMdd_HHmmss}";
                    File.Copy(dbPath, backupPath, overwrite: true);
                }

                try
                {
                    // 2. Zamknij wszystkie połączenia SQLite — wymusza zwolnienie pliku przez EF Core
                    SqliteConnection.ClearAllPools();

                    // 3. Usuń pliki WAL/SHM jeśli istnieją — stare pliki mogłyby zanieczyścić nową bazę
                    var walPath = dbPath + "-wal";
                    var shmPath = dbPath + "-shm";
                    if (File.Exists(walPath)) File.Delete(walPath);
                    if (File.Exists(shmPath)) File.Delete(shmPath);

                    // 4. Skopiuj importowany plik
                    File.Copy(dialog.FileName, dbPath, overwrite: true);

                    // 5. Przełącz journal mode z WAL na DELETE żeby uniknąć problemów z plikami WAL
                    //    i wyczyść blokadę migracji (na wypadek gdyby poprzednia sesja ją zostawiła)
                    SqliteConnection.ClearAllPools();
                    await using (var conn = new SqliteConnection($"Data Source={dbPath}"))
                    {
                        await conn.OpenAsync();
                        await using var pragmaCmd = conn.CreateCommand();
                        pragmaCmd.CommandText = "PRAGMA journal_mode=DELETE; DELETE FROM __EFMigrationsLock WHERE Id = 1";
                        await pragmaCmd.ExecuteNonQueryAsync();
                    }
                    SqliteConnection.ClearAllPools();

                    // 6. Zastosuj migracje (gdy importowana baza pochodzi ze starszej wersji schematu)
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                        await context.Database.MigrateAsync();
                    }
                    SqliteConnection.ClearAllPools();
                }
                catch
                {
                    // Przy błędzie — odtwórz kopię zapasową żeby nie zostawić uszkodzonej bazy
                    SqliteConnection.ClearAllPools();
                    if (backupPath != null && File.Exists(backupPath))
                    {
                        try { File.Copy(backupPath, dbPath, overwrite: true); } catch { /* firewall */ }
                    }
                    throw;
                }

                // 7. Przeładuj wszystkie dane
                await InitializeAsync();

                StatusMessage = "Baza danych zaimportowana pomyślnie.";
                MessageBox.Show(
                    "Baza danych została zaimportowana pomyślnie.\nDane zostały odświeżone.",
                    "Import zakończony", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd importu: {ex.Message}";
                MessageBox.Show($"Błąd podczas importu bazy danych:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Sprawdza magic bytes SQLite: "SQLite format 3\0" (pierwsze 16 bajtów pliku)
        private static bool IsValidSqliteFile(string path)
        {
            try
            {
                Span<byte> buffer = stackalloc byte[16];
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (fs.Read(buffer) < 16) return false;
                ReadOnlySpan<byte> magic = "SQLite format 3\0"u8;
                return buffer.SequenceEqual(magic);
            }
            catch
            {
                return false;
            }
        }

        // Sprawdza integralność bazy SQLite przez PRAGMA integrity_check.
        // Zwraca null gdy OK, lub komunikat błędu gdy baza jest uszkodzona.
        private static string? CheckSqliteIntegrity(string path)
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA integrity_check";
                var result = cmd.ExecuteScalar()?.ToString();
                return result == "ok" ? null : result;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        // ===== TOOLS: EXPORT / IMPORT ATTACHMENTS ZIP =====

        [RelayCommand]
        private async Task ExportAttachmentsZip()
        {
            try
            {
                var attachmentService = _serviceProvider.GetRequiredService<IEventAttachmentService>();
                var folder = attachmentService.GetAttachmentsFolder();
                if (folder == null || !Directory.Exists(folder))
                {
                    MessageBox.Show(
                        "Folder załączników nie jest skonfigurowany lub nie istnieje.\n" +
                        "Skonfiguruj go najpierw przez menu kontekstowe zdarzeń.",
                        "Brak folderu załączników", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var files = Directory.GetFiles(folder);
                if (files.Length == 0)
                {
                    MessageBox.Show("Folder załączników jest pusty — nie ma nic do eksportowania.",
                        "Brak plików", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Title = "Eksportuj załączniki jako ZIP",
                    Filter = "Archiwum ZIP (*.zip)|*.zip",
                    FileName = $"zalaczniki_export_{DateTime.Now:yyyyMMdd_HHmm}.zip",
                    DefaultExt = ".zip"
                };
                if (saveDialog.ShowDialog() != true) return;

                StatusMessage = "Tworzenie archiwum ZIP...";
                var zipPath = saveDialog.FileName;

                await Task.Run(() =>
                {
                    // Wewnątrz ZIP: pliki + manifest z oryginalną ścieżką folderu
                    using var zip = System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Create);

                    // Manifest: zawiera oryginalną ścieżkę folderu na potrzeby importu na innym komputerze
                    var manifest = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        OriginalFolder = folder,
                        ExportedAt = DateTime.Now.ToString("O"),
                        FileCount = files.Length
                    }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    var manifestEntry = zip.CreateEntry("_manifest.json");
                    using (var writer = new System.IO.StreamWriter(manifestEntry.Open()))
                        writer.Write(manifest);

                    // Dodaj wszystkie pliki z folderu
                    foreach (var file in files)
                        zip.CreateEntryFromFile(file, Path.GetFileName(file));
                });

                StatusMessage = $"Archiwum ZIP utworzone: {Path.GetFileName(zipPath)}";
                MessageBox.Show(
                    $"Załączniki ({files.Length} plików) zostały wyeksportowane pomyślnie.\n\nPlik: {zipPath}",
                    "Eksport zakończony", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd eksportu załączników: {ex.Message}";
                await _serviceProvider.GetRequiredService<ILoggingService>().LogErrorAsync($"[ExportAttachmentsZip] {ex.Message}", ex);
                MessageBox.Show($"Błąd podczas eksportu załączników:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ImportAttachmentsZip()
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Title = "Importuj załączniki z ZIP",
                    Filter = "Archiwum ZIP (*.zip)|*.zip",
                    DefaultExt = ".zip"
                };
                if (openDialog.ShowDialog() != true) return;

                var zipPath = openDialog.FileName;

                // Odczytaj manifest z archiwum
                string? originalFolder = null;
                int fileCount = 0;
                await Task.Run(() =>
                {
                    using var zip = System.IO.Compression.ZipFile.OpenRead(zipPath);
                    var manifestEntry = zip.GetEntry("_manifest.json");
                    if (manifestEntry != null)
                    {
                        using var reader = new System.IO.StreamReader(manifestEntry.Open());
                        var json = reader.ReadToEnd();
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("OriginalFolder", out var folderProp))
                            originalFolder = folderProp.GetString();
                        if (doc.RootElement.TryGetProperty("FileCount", out var countProp))
                            fileCount = countProp.GetInt32();
                    }
                });

                // Ustal docelowy folder ekstrakcji
                string? targetFolder = null;

                if (originalFolder != null)
                {
                    // Zapytaj użytkownika: oryginalna ścieżka lub inna?
                    var msg = $"Archiwum zawiera {fileCount} plików załączników.\n\n" +
                              $"Oryginalna ścieżka folderu:\n{originalFolder}\n\n" +
                              $"Czy chcesz wyekstrahować do tej samej lokalizacji?\n\n" +
                              $"Kliknij TAK — ekstrahuj do oryginalnej ścieżki\n" +
                              $"Kliknij NIE — wybierz inny folder\n" +
                              $"Kliknij ANULUJ — przerwij operację";
                    var result = MessageBox.Show(msg, "Wybór folderu docelowego",
                        MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Cancel) return;

                    if (result == MessageBoxResult.Yes)
                    {
                        targetFolder = originalFolder;
                    }
                }

                if (targetFolder == null)
                {
                    // Użytkownik wybrał NIE lub brak manifestu — pokaż FolderBrowserDialog
                    var folderDialog = new System.Windows.Forms.FolderBrowserDialog
                    {
                        Description = "Wybierz folder, do którego zostaną wyekstrahowane załączniki",
                        UseDescriptionForTitle = true,
                        ShowNewFolderButton = true
                    };
                    var currentFolder = _serviceProvider.GetRequiredService<IEventAttachmentService>().GetAttachmentsFolder();
                    if (currentFolder != null)
                        folderDialog.InitialDirectory = currentFolder;

                    if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                        return;
                    targetFolder = folderDialog.SelectedPath;
                }

                // Upewnij się że folder istnieje
                Directory.CreateDirectory(targetFolder);

                // Sprawdź ile plików jest już w folderze docelowym
                var existingFiles = Directory.GetFiles(targetFolder);
                if (existingFiles.Length > 0)
                {
                    var overwriteConfirm = MessageBox.Show(
                        $"Folder docelowy zawiera już {existingFiles.Length} plików.\n" +
                        $"Pliki o tej samej nazwie zostaną nadpisane.\n\nKontynuować?",
                        "Potwierdzenie nadpisania", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (overwriteConfirm != MessageBoxResult.Yes) return;
                }

                StatusMessage = "Ekstrahowanie załączników...";
                int extracted = 0;
                await Task.Run(() =>
                {
                    using var zip = System.IO.Compression.ZipFile.OpenRead(zipPath);
                    foreach (var entry in zip.Entries)
                    {
                        if (entry.Name == "_manifest.json") continue;
                        var destPath = Path.Combine(targetFolder, entry.Name);
                        entry.ExtractToFile(destPath, overwrite: true);
                        extracted++;
                    }
                });

                // Jeśli wybrany folder różni się od skonfigurowanego — zaproponuj aktualizację ustawień
                var currentAttFolder = _serviceProvider.GetRequiredService<IEventAttachmentService>().GetAttachmentsFolder();
                if (!string.Equals(targetFolder, currentAttFolder, StringComparison.OrdinalIgnoreCase))
                {
                    var updateSettings = MessageBox.Show(
                        $"Wyekstrahowano {extracted} plików do:\n{targetFolder}\n\n" +
                        $"Czy ustawić ten folder jako domyślny folder załączników w aplikacji?",
                        "Aktualizacja ustawień", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (updateSettings == MessageBoxResult.Yes)
                    {
                        var prefs = Helpers.AppPreferences.Load();
                        prefs.AttachmentsFolder = targetFolder;
                        prefs.Save();
                    }
                }

                StatusMessage = $"Wyekstrahowano {extracted} załączników do: {targetFolder}";
                MessageBox.Show(
                    $"Import zakończony pomyślnie.\n\nWyekstrahowano {extracted} plików do:\n{targetFolder}",
                    "Import zakończony", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd importu załączników: {ex.Message}";
                await _serviceProvider.GetRequiredService<ILoggingService>().LogErrorAsync($"[ImportAttachmentsZip] {ex.Message}", ex);
                MessageBox.Show($"Błąd podczas importu załączników:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}