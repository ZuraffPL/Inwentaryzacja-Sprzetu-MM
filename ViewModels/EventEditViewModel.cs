using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using InwentaryzacjaSprzetu.Models;
using InwentaryzacjaSprzetu.Services;

namespace InwentaryzacjaSprzetu.ViewModels
{
    public partial class EventEditViewModel : ObservableValidator
    {
        private readonly IInventoryEventService _eventService;
        private readonly IEventAttachmentService _attachmentService;
        private readonly IEquipmentService _equipmentService;
        private readonly ILocationService _locationService;
        private readonly IDepartmentService _departmentService;

        [ObservableProperty]
        private InventoryEventType _eventType;

        partial void OnEventTypeChanged(InventoryEventType value)
        {
            OnPropertyChanged(nameof(IsPurchaseEvent));
            OnPropertyChanged(nameof(IsStatusChangeEvent));
            OnPropertyChanged(nameof(IsDepartmentChangeEvent));
            OnPropertyChanged(nameof(IsInventoryNumberChangeEvent));
            OnPropertyChanged(nameof(IsIpAddressChangeEvent));
            OnPropertyChanged(nameof(IsFiscalNumberChangeEvent));
            OnPropertyChanged(nameof(IsInventoryNumberAssignmentEvent));
            OnPropertyChanged(nameof(IsStandardEquipmentSelector));
            OnPropertyChanged(nameof(IsEquipmentRequired));

            // Gdy typ zmienia się na StatusChange i sprzęt jest już wybrany — ustaw PreviousStatus
            if (value == InventoryEventType.StatusChange && SelectedEquipment != null && !IsEditMode)
            {
                PreviousStatus = SelectedEquipment.Status;
                if (NewStatus == null)
                    NewStatus = SelectedEquipment.Status;
            }

            // Gdy typ zmienia się na DepartmentChange i sprzęt jest już wybrany — ustaw PreviousDepartmentName
            if (value == InventoryEventType.DepartmentChange && SelectedEquipment != null && !IsEditMode)
            {
                PreviousDepartmentName = SelectedEquipment.Department?.Name;
            }

            // Gdy typ zmienia się na zmianę nr inwentarzowego i sprzęt jest już wybrany — auto-wypełnij poprzedni nr
            if (value == InventoryEventType.Audit && SelectedEquipment != null && !IsEditMode)
            {
                PreviousInventoryNumber = SelectedEquipment.InventoryNumber;
            }

            // Gdy typ zmienia się na zmianę adresu IP i sprzęt jest już wybrany — auto-wypełnij poprzedni IP
            if (value == InventoryEventType.IpAddressChange && SelectedEquipment != null && !IsEditMode)
            {
                PreviousIpAddress = SelectedEquipment.IpAddress;
            }

            // Gdy typ zmienia się na zmianę nr unikatowego kasy fiskalnej i sprzęt jest już wybrany — auto-wypełnij poprzedni nr
            if (value == InventoryEventType.FiscalNumberChange && SelectedEquipment != null && !IsEditMode)
            {
                PreviousSerialNumber = SelectedEquipment.SerialNumber;
            }

            // Gdy typ zmienia się na nadanie nr inwentarzowego — wyczyść pola
            if (value == InventoryEventType.InventoryNumberAssignment)
            {
                AssignedEquipmentNumber = null;
                AssignedInventoryNumberPreview = string.Empty;
                NewInventoryNumber = null;
                // Jeśli wybrany sprzęt nie ma braku nr, odłącz go
                if (SelectedEquipment != null && !SelectedEquipment.NoInventoryNumber)
                    SelectedEquipment = null;
            }
        }

        [ObservableProperty]
        [Required(ErrorMessage = "Opis jest wymagany")]
        [StringLength(500, ErrorMessage = "Opis nie może być dłuższy niż 500 znaków")]
        private string _description = string.Empty;

        [ObservableProperty]
        private DateTime _eventDate = DateTime.Now;

        [ObservableProperty]
        private string? _performedBy;

        [ObservableProperty]
        private string? _notes;

        [ObservableProperty]
        private int? _equipmentId;

        partial void OnEquipmentIdChanged(int? value)
        {
            // Synchronizuj SelectedEquipment z listy gdy zmienia się EquipmentId
            if (value == null)
            {
                SelectedEquipment = null;
                return;
            }
            foreach (var e in Equipment)
                if (e.Id == value) { SelectedEquipment = e; return; }
        }

        [ObservableProperty]
        private Equipment? _selectedEquipment;

        partial void OnSelectedEquipmentChanged(Equipment? value)
        {
            EquipmentId = value?.Id;
            if (value != null && IsStatusChangeEvent)
            {
                // Przy nowym zdarzeniu: zapisz aktualny status jako "przed zmianą"
                if (!IsEditMode)
                    PreviousStatus = value.Status;
                // Wstępnie zaproponuj zmianę na ten sam status — użytkownik wybierze docelowy
                if (NewStatus == null)
                    NewStatus = value.Status;
            }
            if (value != null && IsDepartmentChangeEvent && !IsEditMode)
            {
                PreviousDepartmentName = value.Department?.Name;
            }
            // Auto-wypełnij poprzedni nr inwentarzowy przy zmianie numeru
            if (value != null && IsInventoryNumberChangeEvent && !IsEditMode)
            {
                PreviousInventoryNumber = value.InventoryNumber;
                NewInventoryNumber = null; // wyczyść pole nowego numeru
            }
            // Auto-wypełnij poprzedni adres IP przy zmianie IP
            if (value != null && IsIpAddressChangeEvent && !IsEditMode)
            {
                PreviousIpAddress = value.IpAddress;
                NewIpAddress = null;
            }
            // Auto-wypełnij poprzedni nr unikatowy kasy fiskalnej
            if (value != null && IsFiscalNumberChangeEvent && !IsEditMode)
            {
                PreviousSerialNumber = value.SerialNumber;
                NewSerialNumber = null;
            }
            // Automatycznie ustaw lokalizację na podstawie wybranego sprzętu
            if (value != null)
                LocationId = value.LocationId;
            // Gdy typ = nadanie nr inwentarzowego, odśwież podgląd generowanego numeru
            if (value != null && IsInventoryNumberAssignmentEvent)
                RefreshAssignedInventoryNumber();
        }

        [ObservableProperty]
        private int? _locationId;

        /// <summary>Status zdarzenia: Aktywne lub Zakończone (archiwum)</summary>
        [ObservableProperty]
        private EventStatus _eventStatus = EventStatus.Active;

        /// <summary>Wymagana przy zdarzeniu Zakup — synchronizowana z Equipment.PurchasePrice</summary>
        [ObservableProperty]
        private decimal? _purchasePrice;

        /// <summary>Status sprzętu przed zmianą — auto-wypłeniany z Equipment.Status przy tworzeniu, tylko do odczytu w UI</summary>
        [ObservableProperty]
        private EquipmentStatus? _previousStatus;

        /// <summary>Wymagana przy zdarzeniu Zmiana statusu — synchronizowana z Equipment.Status</summary>
        [ObservableProperty]
        private EquipmentStatus? _newStatus;

        /// <summary>Poprzedni dział sprzętu — auto-wypełniany z Equipment.Department przy tworzeniu, tylko do odczytu w UI</summary>
        [ObservableProperty]
        private string? _previousDepartmentName;

        /// <summary>Nowy dział sprzętu — synchronizowany z Equipment.DepartmentId</summary>
        [ObservableProperty]
        private string? _newDepartmentName;

        [ObservableProperty]
        private Department? _selectedNewDepartment;

        partial void OnSelectedNewDepartmentChanged(Department? value)
        {
            NewDepartmentName = value?.Name;
        }

        /// <summary>Poprzedni numer inwentarzowy — auto-wypełniany z Equipment.InventoryNumber; można nadpisać ręcznie (sprzęt z innego pawilonu)</summary>
        [ObservableProperty]
        private string? _previousInventoryNumber;

        /// <summary>Nowy numer inwentarzowy — wpisywany ręcznie; synchronizowany z Equipment.InventoryNumber przy zapisie</summary>
        [ObservableProperty]
        private string? _newInventoryNumber;

        /// <summary>Poprzedni adres IP — auto-wypełniany z Equipment.IpAddress; można nadpisać ręcznie</summary>
        [ObservableProperty]
        private string? _previousIpAddress;

        /// <summary>Nowy adres IP — wpisywany ręcznie; synchronizowany z Equipment.IpAddress przy zapisie</summary>
        [ObservableProperty]
        private string? _newIpAddress;

        /// <summary>Poprzedni nr unikatowy kasy fiskalnej — auto-wypełniany z Equipment.SerialNumber</summary>
        [ObservableProperty]
        private string? _previousSerialNumber;

        /// <summary>Nowy nr unikatowy kasy fiskalnej — wpisywany ręcznie; synchronizowany z Equipment.SerialNumber przy zapisie</summary>
        [ObservableProperty]
        private string? _newSerialNumber;

        /// <summary>Numer sprzętu do nadania nr inwentarzowego (np. 12 → 12/S/87/K)</summary>
        [ObservableProperty]
        private int? _assignedEquipmentNumber;

        partial void OnAssignedEquipmentNumberChanged(int? value)
        {
            RefreshAssignedInventoryNumber();
        }

        /// <summary>Podgląd wygenerowanego numeru inwentarzowego przy zdarzeniu Nadania</summary>
        [ObservableProperty]
        private string _assignedInventoryNumberPreview = string.Empty;

        /// <summary>Generuje podgląd numeru inwentarzowego na podstawie wybranego sprzętu i podanego numeru</summary>
        private void RefreshAssignedInventoryNumber()
        {
            if (!IsInventoryNumberAssignmentEvent
                || SelectedEquipment?.Category == null
                || SelectedEquipment?.Location == null
                || !AssignedEquipmentNumber.HasValue
                || AssignedEquipmentNumber <= 0)
            {
                AssignedInventoryNumberPreview = string.Empty;
                NewInventoryNumber = null;
                return;
            }

            var category = SelectedEquipment!.Category!;
            var location = SelectedEquipment.Location!;

            string generated = category.Code == "LAP"
                ? $"{AssignedEquipmentNumber}/{location.CountryCode}/{location.PavilionCode}"
                : $"{AssignedEquipmentNumber}/S/{location.Code}/{category.Code}";

            AssignedInventoryNumberPreview = generated;
            NewInventoryNumber = generated;
        }

        // Właściwości widoczności pól warunkowych
        public bool IsPurchaseEvent             => EventType == InventoryEventType.Purchase;
        public bool IsStatusChangeEvent         => EventType == InventoryEventType.StatusChange;
        public bool IsDepartmentChangeEvent     => EventType == InventoryEventType.DepartmentChange;
        public bool IsInventoryNumberChangeEvent => EventType == InventoryEventType.Audit;
        public bool IsIpAddressChangeEvent      => EventType == InventoryEventType.IpAddressChange;
        public bool IsFiscalNumberChangeEvent    => EventType == InventoryEventType.FiscalNumberChange;
        public bool IsInventoryNumberAssignmentEvent => EventType == InventoryEventType.InventoryNumberAssignment;
        /// <summary>Pokazuje standardowy selektor sprzętu (wszystkie typy oprócz Nadania nr)</summary>
        public bool IsStandardEquipmentSelector => EventType != InventoryEventType.InventoryNumberAssignment;
        public bool IsEquipmentRequired         => IsStatusChangeEvent || IsDepartmentChangeEvent || IsIpAddressChangeEvent || IsFiscalNumberChangeEvent || IsInventoryNumberAssignmentEvent;

        public ObservableCollection<Equipment> Equipment { get; } = new();
        /// <summary>Sprzęt bez nr inwentarzowego — do zdarzenia 'Nadanie nr inwentarzowego'</summary>
        public ObservableCollection<Equipment> EquipmentWithoutInventoryNumber { get; } = new();
        public ObservableCollection<Location> Locations { get; } = new();
        public ObservableCollection<Department> Departments { get; } = new();

        /// <summary>Załączniki istniejącego zdarzenia (tryb edycji) — ładowane z bazy.</summary>
        public ObservableCollection<EventAttachment> ExistingAttachments { get; } = new();

        /// <summary>Ścieżki plików oczekujących na skopiowanie przy nowym zdarzeniu (tryb dodawania).</summary>
        public ObservableCollection<string> PendingAttachments { get; } = new();

        public InventoryEvent? Event { get; private set; }
        public bool IsEditMode => Event != null;

        public event Action? OnSaveCompleted;
        public event Action? OnCancelRequested;

        public EventEditViewModel(
            IInventoryEventService eventService,
            IEquipmentService equipmentService,
            ILocationService locationService,
            IDepartmentService departmentService,
            IEventAttachmentService attachmentService)
        {
            _eventService = eventService;
            _equipmentService = equipmentService;
            _locationService = locationService;
            _departmentService = departmentService;
            _attachmentService = attachmentService;
        }

        /// <summary>Inicjalizacja dla trybu edycji istniejącego zdarzenia</summary>
        public async Task InitializeAsync(InventoryEvent? eventToEdit = null)
        {
            await LoadEquipmentAsync();
            await LoadLocationsAsync();
            await LoadDepartmentsAsync();

            if (eventToEdit != null)
            {
                Event = eventToEdit;
                EventType    = eventToEdit.EventType;
                Description  = eventToEdit.Description;
                EventDate    = eventToEdit.EventDate;
                PerformedBy  = eventToEdit.PerformedBy;
                Notes        = eventToEdit.Notes;
                LocationId     = eventToEdit.LocationId;
                PurchasePrice  = eventToEdit.PurchasePrice;
                PreviousStatus = eventToEdit.PreviousStatus;
                NewStatus      = eventToEdit.NewStatus;
                PreviousDepartmentName = eventToEdit.PreviousDepartmentName;
                NewDepartmentName      = eventToEdit.NewDepartmentName;
                PreviousInventoryNumber = eventToEdit.PreviousInventoryNumber;
                NewInventoryNumber      = eventToEdit.NewInventoryNumber;
                PreviousIpAddress       = eventToEdit.PreviousIpAddress;
                NewIpAddress            = eventToEdit.NewIpAddress;
                PreviousSerialNumber    = eventToEdit.PreviousSerialNumber;
                NewSerialNumber         = eventToEdit.NewSerialNumber;
                EventStatus            = eventToEdit.EventStatus;
                // Ustaw SelectedNewDepartment z listy działów na podstawie NewDepartmentName
                if (!string.IsNullOrEmpty(eventToEdit.NewDepartmentName))
                    SelectedNewDepartment = Departments.FirstOrDefault(d => d.Name == eventToEdit.NewDepartmentName);
                // Ustaw SelectedEquipment — wyzwoli OnSelectedEquipmentChanged
                EquipmentId    = eventToEdit.EquipmentId;

                // Załaduj istniejące załączniki
                await LoadExistingAttachmentsAsync();
            }
        }

        /// <summary>Inicjalizacja przez kontekst z widoku sprzętu — pre-populate danych sprzętu</summary>
        public async Task InitializeForEquipmentAsync(Models.Equipment equipment, InventoryEventType defaultType = InventoryEventType.Purchase)
        {
            await LoadEquipmentAsync();
            await LoadLocationsAsync();
            await LoadDepartmentsAsync();

            EventType   = defaultType;
            LocationId  = equipment.LocationId;

            // Tylko ustaw EquipmentId — wyzwoli OnEquipmentIdChanged i OnSelectedEquipmentChanged
            EquipmentId = equipment.Id;

            // Dla zakupu pre-populate ceną jeśli jest już wpisana
            if (defaultType == InventoryEventType.Purchase && equipment.PurchasePrice.HasValue)
                PurchasePrice = equipment.PurchasePrice;

            // Dla zmiany statusu ustaw obecny status sprzętu jako punkt wyjścia
            if (defaultType == InventoryEventType.StatusChange)
                NewStatus = equipment.Status;

            // Dla zmiany działu ustaw obecny dział sprzętu
            if (defaultType == InventoryEventType.DepartmentChange)
                PreviousDepartmentName = equipment.Department?.Name;

            // Dla zmiany nr inwentarzowego auto-wypełnij poprzedni nr
            if (defaultType == InventoryEventType.Audit)
                PreviousInventoryNumber = equipment.InventoryNumber;

            // Dla zmiany adresu IP auto-wypełnij poprzedni IP
            if (defaultType == InventoryEventType.IpAddressChange)
                PreviousIpAddress = equipment.IpAddress;

            // Dla zmiany nr unikatowego kasy fiskalnej auto-wypełnij poprzedni nr
            if (defaultType == InventoryEventType.FiscalNumberChange)
                PreviousSerialNumber = equipment.SerialNumber;
        }

        private async Task LoadEquipmentAsync()
        {
            try
            {
                var equipment = await _equipmentService.GetAllAsync();
                Equipment.Clear();
                EquipmentWithoutInventoryNumber.Clear();
                foreach (var item in equipment)
                {
                    Equipment.Add(item);
                    if (item.NoInventoryNumber)
                        EquipmentWithoutInventoryNumber.Add(item);
                }
            }
            catch { Equipment.Clear(); EquipmentWithoutInventoryNumber.Clear(); }
        }

        private async Task LoadLocationsAsync()
        {
            try
            {
                var locations = await _locationService.GetAllAsync();
                Locations.Clear();
                foreach (var location in locations)
                    Locations.Add(location);
            }
            catch { Locations.Clear(); }
        }

        private async Task LoadDepartmentsAsync()
        {
            try
            {
                var departments = await _departmentService.GetAllAsync();
                Departments.Clear();
                foreach (var department in departments)
                    Departments.Add(department);
            }
            catch { Departments.Clear(); }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            // Walidacja pól warunkowych
            if (IsPurchaseEvent && (PurchasePrice == null || PurchasePrice <= 0))
            {
                System.Windows.MessageBox.Show(
                    "Dla zdarzenia zakupu wymagana jest cena zakupu większa od 0.",
                    "Walidacja", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (IsStatusChangeEvent && SelectedEquipment == null)
            {
                System.Windows.MessageBox.Show(
                    "Dla zmiany statusu wymagane jest wybranie sprzętu.",
                    "Walidacja", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (IsStatusChangeEvent && NewStatus == null)
            {
                System.Windows.MessageBox.Show(
                    "Dla zmiany statusu wymagane jest wybranie nowego statusu.",
                    "Walidacja", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (IsDepartmentChangeEvent && SelectedEquipment == null)
            {
                System.Windows.MessageBox.Show(
                    "Dla zmiany działu wymagane jest wybranie sprzętu.",
                    "Walidacja", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (IsDepartmentChangeEvent && SelectedNewDepartment == null)
            {
                System.Windows.MessageBox.Show(
                    "Dla zmiany działu wymagane jest wybranie nowego działu.",
                    "Walidacja", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (IsInventoryNumberChangeEvent && SelectedEquipment == null && string.IsNullOrWhiteSpace(PreviousInventoryNumber))
            {
                System.Windows.MessageBox.Show(
                    "Podaj poprzedni numer inwentarzowy (sprzęt nie jest wybrany z listy — wpisz numer ręcznie).",
                    "Walidacja", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (IsInventoryNumberChangeEvent && string.IsNullOrWhiteSpace(NewInventoryNumber))
            {
                System.Windows.MessageBox.Show(
                    "Dla zmiany nr inwentarzowego wymagane jest podanie nowego numeru inwentarzowego.",
                    "Walidacja", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (IsIpAddressChangeEvent && SelectedEquipment == null)
            {
                System.Windows.MessageBox.Show(
                    "Dla zmiany adresu IP wymagane jest wybranie sprzętu.",
                    "Walidacja", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (IsIpAddressChangeEvent && string.IsNullOrWhiteSpace(NewIpAddress))
            {
                System.Windows.MessageBox.Show(
                    "Dla zmiany adresu IP wymagane jest podanie nowego adresu IP.",
                    "Walidacja", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (IsFiscalNumberChangeEvent && SelectedEquipment == null)
            {
                System.Windows.MessageBox.Show(
                    "Dla zmiany nr unikatowego kasy fiskalnej wymagane jest wybranie sprzętu.",
                    "Walidacja", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (IsFiscalNumberChangeEvent && string.IsNullOrWhiteSpace(NewSerialNumber))
            {
                System.Windows.MessageBox.Show(
                    "Dla zmiany nr unikatowego wymagane jest podanie nowego numeru.",
                    "Walidacja", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (IsInventoryNumberAssignmentEvent && SelectedEquipment == null)
            {
                System.Windows.MessageBox.Show(
                    "Dla nadania nr inwentarzowego wymagane jest wybranie sprzętu bez numeru.",
                    "Walidacja", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (IsInventoryNumberAssignmentEvent && string.IsNullOrWhiteSpace(AssignedInventoryNumberPreview))
            {
                System.Windows.MessageBox.Show(
                    "Wprowadź numer sprzętu, aby wygenerować numer inwentarzowy.",
                    "Walidacja", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (IsEditMode)
                {
                    Event!.EventType      = EventType;
                    Event.Description     = Description;
                    Event.EventDate       = EventDate;
                    Event.PerformedBy     = PerformedBy;
                    Event.Notes           = Notes;
                    Event.EquipmentId     = EquipmentId;
                    Event.LocationId      = LocationId;
                    Event.PurchasePrice   = PurchasePrice;
                    Event.PreviousStatus  = PreviousStatus;
                    Event.NewStatus       = NewStatus;
                    Event.PreviousDepartmentName = PreviousDepartmentName;
                    Event.NewDepartmentName      = NewDepartmentName;
                    Event.PreviousInventoryNumber = PreviousInventoryNumber;
                    Event.NewInventoryNumber      = NewInventoryNumber;
                    Event.PreviousIpAddress       = PreviousIpAddress;
                    Event.NewIpAddress            = NewIpAddress;
                    Event.PreviousSerialNumber    = PreviousSerialNumber;
                    Event.NewSerialNumber         = NewSerialNumber;
                    Event.EventStatus            = EventStatus;

                    await _eventService.UpdateAsync(Event);
                }
                else
                {
                    var newEvent = new InventoryEvent
                    {
                        EventType      = EventType,
                        Description    = Description,
                        EventDate      = EventDate,
                        PerformedBy    = PerformedBy,
                        Notes          = Notes,
                        EquipmentId    = EquipmentId,
                        LocationId     = LocationId,
                        PurchasePrice  = PurchasePrice,
                        PreviousStatus = PreviousStatus,
                        NewStatus      = NewStatus,
                        PreviousDepartmentName = PreviousDepartmentName,
                        NewDepartmentName      = NewDepartmentName,
                        PreviousInventoryNumber = PreviousInventoryNumber,
                        NewInventoryNumber      = NewInventoryNumber,
                        PreviousIpAddress       = PreviousIpAddress,
                        NewIpAddress            = NewIpAddress,
                        PreviousSerialNumber    = PreviousSerialNumber,
                        NewSerialNumber         = NewSerialNumber,
                        EventStatus            = EventStatus
                    };

                    await _eventService.CreateAsync(newEvent);

                    // Skopiuj oczekujące załączniki do nowo utworzonego zdarzenia
                    foreach (var pendingPath in PendingAttachments)
                    {
                        if (File.Exists(pendingPath))
                            await _attachmentService.AddAsync(newEvent.Id, pendingPath);
                    }
                    PendingAttachments.Clear();
                }

                // Synchronizacja obustronna ze sprzętem
                await SyncEquipmentAsync();

                OnSaveCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Błąd podczas zapisywania zdarzenia: {ex.Message}",
                    "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Aktualizuje powiązany rekord sprzętu na podstawie typu zdarzenia:
        /// - Purchase  → Equipment.PurchasePrice, Equipment.PurchaseDate
        /// - StatusChange → Equipment.Status
        /// </summary>
        private async Task SyncEquipmentAsync()
        {
            if (SelectedEquipment == null) return;

            try
            {
                var equipment = await _equipmentService.GetByIdAsync(SelectedEquipment.Id);
                if (equipment == null) return;

                bool changed = false;

                if (IsPurchaseEvent && PurchasePrice.HasValue)
                {
                    equipment.PurchasePrice = PurchasePrice;
                    equipment.PurchaseDate  = EventDate;
                    changed = true;
                }

                if (IsStatusChangeEvent && NewStatus.HasValue && equipment.Status != NewStatus.Value)
                {
                    equipment.Status = NewStatus.Value;
                    changed = true;
                }

                if (IsDepartmentChangeEvent && SelectedNewDepartment != null && equipment.DepartmentId != SelectedNewDepartment.Id)
                {
                    equipment.DepartmentId = SelectedNewDepartment.Id;
                    changed = true;
                }

                if (IsInventoryNumberChangeEvent && !string.IsNullOrWhiteSpace(NewInventoryNumber)
                    && equipment.InventoryNumber != NewInventoryNumber)
                {
                    equipment.InventoryNumber = NewInventoryNumber.Trim();
                    changed = true;
                }

                if (IsIpAddressChangeEvent && !string.IsNullOrWhiteSpace(NewIpAddress)
                    && equipment.IpAddress != NewIpAddress)
                {
                    equipment.IpAddress = NewIpAddress.Trim();
                    changed = true;
                }

                if (IsFiscalNumberChangeEvent && !string.IsNullOrWhiteSpace(NewSerialNumber)
                    && equipment.SerialNumber != NewSerialNumber)
                {
                    equipment.SerialNumber = NewSerialNumber.Trim();
                    changed = true;
                }

                if (IsInventoryNumberAssignmentEvent && !string.IsNullOrWhiteSpace(NewInventoryNumber)
                    && equipment.InventoryNumber != NewInventoryNumber)
                {
                    equipment.InventoryNumber = NewInventoryNumber.Trim();
                    equipment.NoInventoryNumber = false;
                    changed = true;
                }

                if (changed)
                    await _equipmentService.UpdateAsync(equipment);
            }
            catch
            {
                // Nie przerywaj — zdarzenie zostało zapisane, synchronizacja sprzętu jest best-effort
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            OnCancelRequested?.Invoke();
        }

        // ===== Załączniki =====

        private async Task LoadExistingAttachmentsAsync()
        {
            if (Event == null) return;
            ExistingAttachments.Clear();
            var items = await _attachmentService.GetByEventIdAsync(Event.Id);
            foreach (var item in items)
                ExistingAttachments.Add(item);
        }

        [RelayCommand]
        private async Task AddAttachmentAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title  = "Wybierz plik załącznika",
                Filter = "Obsługiwane pliki (*.jpg;*.jpeg;*.png;*.webp;*.pdf)|*.jpg;*.jpeg;*.png;*.webp;*.pdf" +
                         "|Obrazy (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp" +
                         "|Dokumenty PDF (*.pdf)|*.pdf" +
                         "|Wszystkie pliki (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true) return;

            if (IsEditMode)
            {
                // Tryb edycji: zapisz natychmiast w bazie
                var result = await _attachmentService.AddAsync(Event!.Id, dialog.FileName);
                if (result != null)
                    await LoadExistingAttachmentsAsync();
            }
            else
            {
                // Tryb dodawania: dodaj do listy oczekujących
                PendingAttachments.Add(dialog.FileName);
            }
        }

        [RelayCommand]
        private void RemovePendingAttachment(string? path)
        {
            if (path != null)
                PendingAttachments.Remove(path);
        }

        [RelayCommand]
        private async Task RemoveExistingAttachmentAsync(EventAttachment? attachment)
        {
            if (attachment == null) return;
            var confirm = System.Windows.MessageBox.Show(
                $"Czy na pewno usunąć załącznik:\n{attachment.OriginalFileName}?\n\nPlik zostanie trwale usunięty.",
                "Usuń załącznik", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (confirm != System.Windows.MessageBoxResult.Yes) return;
            await _attachmentService.DeleteAsync(attachment.Id);
            await LoadExistingAttachmentsAsync();
        }
    }
}
