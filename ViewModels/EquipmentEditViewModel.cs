using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwentaryzacjaSprzetu.Models;
using InwentaryzacjaSprzetu.Services;

namespace InwentaryzacjaSprzetu.ViewModels
{
    public partial class EquipmentEditViewModel : ObservableObject
    {
        private readonly IEquipmentService _equipmentService;
        private readonly ICategoryService _categoryService;
        private readonly ILocationService _locationService;
        private readonly IDepartmentService _departmentService;

        // Flaga blokująca auto-ładowanie komputerów podczas InitializeAsync
        private bool _isInitializing = false;

        [ObservableProperty]
        private string _inventoryNumber = string.Empty;

        [ObservableProperty]
        private int? _equipmentNumber;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string? _description;

        [ObservableProperty]
        private string? _brand;

        [ObservableProperty]
        private string? _model;

        [ObservableProperty]
        private string? _serialNumber;

        [ObservableProperty]
        private string? _ipAddress;

        [ObservableProperty]
        private int? _departmentId;

        [ObservableProperty]
        private Department? _selectedDepartment;

        [ObservableProperty]
        private int? _connectedToEquipmentId;

        [ObservableProperty]
        private Equipment? _selectedConnectedEquipment;

        [ObservableProperty]
        private string? _customDepartmentName;

        [ObservableProperty]
        private string? _locationDetails;

        [ObservableProperty]
        private decimal? _purchasePrice;

        [ObservableProperty]
        private DateTime? _purchaseDate;

        [ObservableProperty]
        private DateTime? _warrantyEndDate;

        [ObservableProperty]
        private string? _notes;

        [ObservableProperty]
        private EquipmentStatus _status = EquipmentStatus.Active;

        [ObservableProperty]
        private Category? _selectedCategory;

        [ObservableProperty]
        private Location? _selectedLocation;

        [ObservableProperty]
        private string _validationMessage = string.Empty;

        [ObservableProperty]
        private bool _noInventoryNumber = false;

        /// <summary>Czy pole Numer sprzętu jest aktywne (dezaktywowane gdy NoInventoryNumber = true)</summary>
        public bool IsEquipmentNumberEditable => !NoInventoryNumber;

        public string DialogTitle => IsEditMode ? "Edycja sprzętu" : "Dodaj sprzęt";

        partial void OnNoInventoryNumberChanged(bool value)
        {
            OnPropertyChanged(nameof(IsEquipmentNumberEditable));
            RefreshInventoryNumberPreview();
        }

        // Sentinel oznaczający brak podłączenia (N/A) — nie jest realnym sprzętem
        private static readonly Equipment _noComputerSentinel = new Equipment
        {
            Id = -1,
            InventoryNumber = "— Brak / N/A —",
            Name = string.Empty,
            CategoryId = 0,
            LocationId = 0
        };

        // Sentinel oznaczający brak działu (N/A)
        public static readonly Department NoDepartmentSentinel = new Department
        {
            Id = 0,
            Code = "__NA__",
            Name = "— N/A (brak działu) —"
        };

        // Sentinel dla opcji definiowania własnego działu
        public static readonly Department CustomDepartmentSentinel = new Department
        {
            Id = -1,
            Code = "__CUSTOM__",
            Name = "--- Zdefiniuj własny dział ---"
        };

        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<Location> Locations { get; } = new();
        public ObservableCollection<Department> Departments { get; } = new();
        public ObservableCollection<Department> DepartmentOptions { get; } = new();
        public ObservableCollection<Equipment> Computers { get; } = new();

        /// <summary>Lista sprzętu z wybranej kategorii — do szybkiego kopiowania danych przy dodawaniu.</summary>
        public ObservableCollection<Equipment> EquipmentTemplates { get; } = new();

        [ObservableProperty]
        private Equipment? _selectedEquipmentTemplate;

        public Equipment? Equipment { get; private set; }
        public bool IsEditMode => Equipment != null;
        public bool IsMonitorCategory => SelectedCategory?.Code == "M";
        public bool IsLaptopCategory => SelectedCategory?.Code == "LAP";
        public bool IsComputerCategory => SelectedCategory?.Code == "K";
        public bool IsConnectedToComputerCategory =>
            SelectedCategory?.Code is "M" or "SC" or "D" or "U" or "SR" or "F" or "TE" or "KK" or "KD";
        public bool IsNoIpCategory =>
            SelectedCategory?.Code is "M" or "SC" or "KK" or "KD" or "N" or "TE";

        // Dynamiczne etykiety pól
        // K=Komputery: System/RAM/Procesor, M=Monitory: Marka/Model/Przekątna, F=Kasy Fiskalne: Marka/Model/Nr unikatowy
        public string BrandLabel      => IsComputerCategory ? "System:"      : "Marka:";
        public string ModelLabel      => IsComputerCategory ? "RAM:"         : "Model:";
        public string SerialNumberLabel => SelectedCategory?.Code switch
        {
            "K"  => "Procesor:",
            "M"  => "Przekątna:",
            "F"  => "Nr unikatowy:",
            "TK" => "IMEI:",
            _    => "Numer seryjny:"
        };

        public EquipmentEditViewModel(
            IEquipmentService equipmentService,
            ICategoryService categoryService,
            ILocationService locationService,
            IDepartmentService departmentService)
        {
            _equipmentService = equipmentService;
            _categoryService = categoryService;
            _locationService = locationService;
            _departmentService = departmentService;
        }

        /// <summary>Inicjalizuje VMdla nowego sprzętu z pre-ustawioną kategorią i opcjonalnie lokalizacją.</summary>
        public async Task InitializeForCategoryAsync(Category category, Location? location = null)
        {
            await InitializeAsync();
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == category.Id);
            if (location != null)
                SelectedLocation = Locations.FirstOrDefault(l => l.Id == location.Id);
        }

        public async Task InitializeAsync(Equipment? equipment = null)
        {
            _isInitializing = true;
            Equipment = equipment;

            // Ładowanie kategorii i lokalizacji
            var categories = await _categoryService.GetAllAsync();
            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }

            var locations = await _locationService.GetAllAsync();
            Locations.Clear();
            foreach (var location in locations)
            {
                Locations.Add(location);
            }

            // Ładowanie działów
            var departments = await _departmentService.GetAllAsync();
            Departments.Clear();
            DepartmentOptions.Clear();

            // Pierwsza pozycja: brak działu (N/A)
            DepartmentOptions.Add(NoDepartmentSentinel);

            foreach (var department in departments)
            {
                Departments.Add(department);
                DepartmentOptions.Add(department);
            }
            
            // Dodaj opcję definiowania własnego działu
            DepartmentOptions.Add(CustomDepartmentSentinel);

            // Jeśli edytujemy, wypełnij pola
            if (equipment != null)
            {
                InventoryNumber = equipment.InventoryNumber;
                NoInventoryNumber = equipment.NoInventoryNumber;
                // Wyciągnij numer sprzętu z numeru inwentarzowego (pierwsza część przed "/S/")
                var parts = equipment.InventoryNumber.Split('/');
                if (parts.Length > 0 && int.TryParse(parts[0], out var equipmentNum))
                {
                    EquipmentNumber = equipmentNum;
                }
                Name = equipment.Name;
                Description = equipment.Description;
                Brand = equipment.Brand;
                Model = equipment.Model;
                SerialNumber = equipment.SerialNumber;
                IpAddress = equipment.IpAddress;
                DepartmentId = equipment.DepartmentId;
                SelectedDepartment = equipment.DepartmentId.HasValue
                    ? Departments.FirstOrDefault(d => d.Id == equipment.DepartmentId)
                    : NoDepartmentSentinel;
                ConnectedToEquipmentId = equipment.ConnectedToEquipmentId;
                LocationDetails = equipment.LocationDetails;
                PurchasePrice = equipment.PurchasePrice;
                PurchaseDate = equipment.PurchaseDate;
                WarrantyEndDate = equipment.WarrantyEndDate;
                Notes = equipment.Notes;
                Status = equipment.Status;
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == equipment.CategoryId);
                SelectedLocation = Locations.FirstOrDefault(l => l.Id == equipment.LocationId);
            }
            else
            {
                // Nowy sprzęt — domyślnie brak działu (N/A)
                SelectedDepartment = NoDepartmentSentinel;
            }

            // Jawnie załaduj komputery (jeśli kategoria tego wymaga) i ustaw SelectedConnectedEquipment
            if (IsConnectedToComputerCategory)
            {
                await LoadComputersAsync();
                if (equipment?.ConnectedToEquipmentId.HasValue == true)
                    SelectedConnectedEquipment = Computers.FirstOrDefault(c => c.Id == equipment.ConnectedToEquipmentId);
                else
                    SelectedConnectedEquipment = _noComputerSentinel;
            }

            // Załaduj szablony sprzętu z tej samej kategorii (tylko przy dodawaniu nowego)
            if (equipment == null && SelectedCategory != null)
                await LoadEquipmentTemplatesAsync(SelectedCategory);

            _isInitializing = false;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (!ValidateInput())
                return;

            try
            {
                if (Equipment == null) // Nowy sprzęt
                {
                    // Generuj numer inwentarzowy z ręcznie wprowadzonym numerem sprzętu
                    var inventoryNumber = GenerateInventoryNumber();

                    var newEquipment = new Equipment
                    {
                        InventoryNumber = inventoryNumber,
                        NoInventoryNumber = NoInventoryNumber,
                        Name = Name,
                        Description = Description,
                        Brand = Brand,
                        Model = Model,
                        SerialNumber = SerialNumber,
                        IpAddress = IpAddress,
                        DepartmentId = SelectedDepartment?.Id > 0 ? SelectedDepartment?.Id : null,
                        ConnectedToEquipmentId = SelectedConnectedEquipment?.Id > 0 ? SelectedConnectedEquipment.Id : null,
                        LocationDetails = LocationDetails,
                        PurchasePrice = PurchasePrice,
                        PurchaseDate = PurchaseDate,
                        WarrantyEndDate = WarrantyEndDate,
                        Notes = Notes,
                        Status = Status,
                        CategoryId = SelectedCategory?.Id ?? 0,
                        LocationId = SelectedLocation?.Id ?? 0,
                        CreatedDate = DateTime.Now
                    };

                    await _equipmentService.AddAsync(newEquipment);
                }
                else // Edycja
                {
                    // Przy edycji, zaktualizuj numer inwentarzowy jeśli zmieniono numer sprzętu, kategorię lub lokalizację
                    Equipment.InventoryNumber = GenerateInventoryNumber();
                    Equipment.NoInventoryNumber = NoInventoryNumber;
                    Equipment.Name = Name;
                    Equipment.Description = Description;
                    Equipment.Brand = Brand;
                    Equipment.Model = Model;
                    Equipment.SerialNumber = SerialNumber;
                    Equipment.IpAddress = IpAddress;
                    Equipment.DepartmentId = SelectedDepartment?.Id > 0 ? SelectedDepartment?.Id : null;
                    Equipment.ConnectedToEquipmentId = SelectedConnectedEquipment?.Id > 0 ? SelectedConnectedEquipment.Id : null;
                    Equipment.LocationDetails = LocationDetails;
                    Equipment.PurchasePrice = PurchasePrice;
                    Equipment.PurchaseDate = PurchaseDate;
                    Equipment.WarrantyEndDate = WarrantyEndDate;
                    Equipment.Notes = Notes;
                    Equipment.Status = Status;
                    Equipment.CategoryId = SelectedCategory?.Id ?? 0;
                    Equipment.LocationId = SelectedLocation?.Id ?? 0;
                    Equipment.LastModifiedDate = DateTime.Now;

                    await _equipmentService.UpdateAsync(Equipment);
                }

                OnSaveCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Błąd podczas zapisywania: {ex.Message}";
            }
        }

        private string GenerateInventoryNumber()
        {
            if (NoInventoryNumber)
                return string.Empty;

            if (SelectedCategory == null || SelectedLocation == null)
                throw new InvalidOperationException("Kategoria i lokalizacja muszą być wybrane");

            if (!EquipmentNumber.HasValue)
                throw new InvalidOperationException("Numer sprzętu musi być wprowadzony");

            // Format Laptopy: nr/KodKraju/KodPawilonu, np. 1/PL/ZM
            if (SelectedCategory.Code == "LAP")
                return $"{EquipmentNumber}/{SelectedLocation.CountryCode}/{SelectedLocation.PavilionCode}";

            // Format standardowy: nr_sprzętu/S/kod_lokalizacji/kod_kategorii, np. 12/S/87/K
            return $"{EquipmentNumber}/S/{SelectedLocation.Code}/{SelectedCategory.Code}";
        }

        private bool ValidateInput()
        {
            if (SelectedCategory == null)
            {
                ValidationMessage = "Wybierz kategorię";
                return false;
            }

            if (SelectedLocation == null)
            {
                ValidationMessage = "Wybierz lokalizację";
                return false;
            }

            if (!NoInventoryNumber && (!EquipmentNumber.HasValue || EquipmentNumber <= 0))
            {
                ValidationMessage = "Wprowadź prawidłowy numer sprzętu";
                return false;
            }

            ValidationMessage = string.Empty;
            return true;
        }

        partial void OnEquipmentNumberChanged(int? value)
        {
            RefreshInventoryNumberPreview();
        }

        partial void OnSelectedLocationChanged(Location? value)
        {
            RefreshInventoryNumberPreview();
        }

        partial void OnSelectedCategoryChanged(Category? value)
        {
            OnPropertyChanged(nameof(IsMonitorCategory));
            OnPropertyChanged(nameof(IsLaptopCategory));
            OnPropertyChanged(nameof(IsComputerCategory));
            OnPropertyChanged(nameof(IsConnectedToComputerCategory));
            OnPropertyChanged(nameof(IsNoIpCategory));
            OnPropertyChanged(nameof(BrandLabel));
            OnPropertyChanged(nameof(ModelLabel));
            OnPropertyChanged(nameof(SerialNumberLabel));
            RefreshInventoryNumberPreview();

            if (!_isInitializing)
            {
                // Załaduj komputery jeśli wybrano kategorię z podpięciem do komputera
                if (value?.Code is "M" or "SC" or "D" or "U" or "SR" or "F" or "TE" or "KK" or "KD")
                {
                    _ = LoadComputersAndSelectDefault();
                }

                // Załaduj szablony sprzętu z tej samej kategorii (tylko przy dodawaniu)
                if (Equipment == null)
                    _ = LoadEquipmentTemplatesAsync(value);
            }
        }

        partial void OnSelectedEquipmentTemplateChanged(Equipment? value)
        {
            if (value == null) return;
            Name = value.Name;
            Brand = value.Brand;
            Model = value.Model;
            SerialNumber = value.SerialNumber;
        }

        private void RefreshInventoryNumberPreview()
        {
            // Podgląd tylko przy dodawaniu nowego sprzętu
            if (Equipment != null)
                return;

            if (NoInventoryNumber)
            {
                InventoryNumber = string.Empty;
                return;
            }

            if (EquipmentNumber.HasValue && EquipmentNumber > 0
                && SelectedCategory != null && SelectedLocation != null)
            {
                if (SelectedCategory.Code == "LAP")
                    InventoryNumber = $"{EquipmentNumber}/{SelectedLocation.CountryCode}/{SelectedLocation.PavilionCode}";
                else
                    InventoryNumber = $"{EquipmentNumber}/S/{SelectedLocation.Code}/{SelectedCategory.Code}";
            }
            else
            {
                InventoryNumber = string.Empty;
            }
        }

        private async Task LoadComputersAndSelectDefault()
        {
            await LoadComputersAsync();
            // Po załadowaniu listy domyślnie zaznacz sentinel (N/A) jeśli żaden komputer nie jest wybrany
            if (SelectedConnectedEquipment == null || SelectedConnectedEquipment.Id <= 0)
                SelectedConnectedEquipment = _noComputerSentinel;
        }

        private async Task LoadEquipmentTemplatesAsync(Category? category)
        {
            EquipmentTemplates.Clear();
            SelectedEquipmentTemplate = null;
            if (category == null) return;
            try
            {
                var items = await _equipmentService.GetByCategoryAsync(category.Id);
                foreach (var item in items.OrderBy(e => e.InventoryNumberSortKey).ThenBy(e => e.InventoryNumber))
                    EquipmentTemplates.Add(item);
            }
            catch (Exception)
            {
                // Błąd ładowania — lista pozostanie pusta
            }
        }

        private async Task LoadComputersAsync()
        {
            try
            {
                // Załaduj komputery (K) i serwery (S) — do obu można podłączać peryferia
                var compCategoryId = Categories.FirstOrDefault(c => c.Code == "K")?.Id ?? 0;
                var srvCategoryId  = Categories.FirstOrDefault(c => c.Code == "S")?.Id ?? 0;

                var computers = compCategoryId > 0
                    ? await _equipmentService.GetByCategoryAsync(compCategoryId)
                    : Enumerable.Empty<Equipment>();

                var servers = srvCategoryId > 0
                    ? await _equipmentService.GetByCategoryAsync(srvCategoryId)
                    : Enumerable.Empty<Equipment>();

                Computers.Clear();
                Computers.Add(_noComputerSentinel); // opcja N/A — brak podłączenia
                foreach (var item in computers.Concat(servers).OrderBy(e => e.InventoryNumber))
                    Computers.Add(item);
            }
            catch (Exception)
            {
                // Błąd ładowania — lista pozostanie pusta
            }
        }

        public event Action? OnSaveCompleted;
    }
}