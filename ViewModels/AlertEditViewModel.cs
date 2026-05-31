using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwentaryzacjaSprzetu.Models;
using InwentaryzacjaSprzetu.Services;

namespace InwentaryzacjaSprzetu.ViewModels
{
    public partial class AlertEditViewModel : ObservableObject
    {
        private readonly IAlertService _alertService;
        private readonly IEquipmentService _equipmentService;
        private readonly ICategoryService _categoryService;

        private Alert? _editedAlert;
        private bool _isEditMode;

        // ─── Pola formularza ──────────────────────────────────────────────────

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string? _description;

        [ObservableProperty]
        private string _content = string.Empty;

        [ObservableProperty]
        private DateTime _triggerDate = DateTime.Today;

        // ─── Zakres (radio buttons) ───────────────────────────────────────────

        [ObservableProperty]
        private bool _forEquipment = true;

        partial void OnForEquipmentChanged(bool value)
        {
            if (value) { _forCategory = false; _forGeneral = false; OnPropertyChanged(nameof(ForCategory)); OnPropertyChanged(nameof(ForGeneral)); }
        }

        [ObservableProperty]
        private bool _forCategory;

        partial void OnForCategoryChanged(bool value)
        {
            if (value) { _forEquipment = false; _forGeneral = false; OnPropertyChanged(nameof(ForEquipment)); OnPropertyChanged(nameof(ForGeneral)); }
        }

        [ObservableProperty]
        private bool _forGeneral;

        partial void OnForGeneralChanged(bool value)
        {
            if (value) { _forEquipment = false; _forCategory = false; OnPropertyChanged(nameof(ForEquipment)); OnPropertyChanged(nameof(ForCategory)); }
        }

        // ─── Selekcja celu ────────────────────────────────────────────────────

        [ObservableProperty]
        private Equipment? _selectedEquipment;

        [ObservableProperty]
        private Category? _selectedCategory;

        [ObservableProperty]
        private string _equipmentSearch = string.Empty;

        partial void OnEquipmentSearchChanged(string value) => FilterEquipment();

        public ObservableCollection<Equipment> AllEquipment { get; } = new();
        public ObservableCollection<Equipment> FilteredEquipment { get; } = new();
        public ObservableCollection<Category> AllCategories { get; } = new();

        // ─── Metadata ─────────────────────────────────────────────────────────

        public string DialogTitle => _isEditMode ? "Edycja powiadomienia" : "Nowe powiadomienie";

        public event Action? OnSaveCompleted;
        public event Action? OnCancelRequested;

        // ─── Constructor ──────────────────────────────────────────────────────

        public AlertEditViewModel(
            IAlertService alertService,
            IEquipmentService equipmentService,
            ICategoryService categoryService)
        {
            _alertService    = alertService;
            _equipmentService = equipmentService;
            _categoryService  = categoryService;
        }

        // ─── Inicjalizacja ────────────────────────────────────────────────────

        public async Task InitializeAsync(Alert? alert = null)
        {
            _editedAlert = alert;
            _isEditMode  = alert != null;
            OnPropertyChanged(nameof(DialogTitle));

            // Załaduj sprzęt i kategorie
            var equipment  = (await _equipmentService.GetAllAsync()).OrderBy(e => e.InventoryNumber);
            var categories = (await _categoryService.GetAllAsync()).OrderBy(c => c.SortOrder);

            AllEquipment.Clear();
            foreach (var e in equipment) AllEquipment.Add(e);

            AllCategories.Clear();
            foreach (var c in categories) AllCategories.Add(c);

            FilterEquipment();

            if (alert != null)
            {
                // Tryb edycji
                Name        = alert.Name;
                Description = alert.Description;
                Content     = alert.Content;
                TriggerDate = alert.TriggerDate;

                if (alert.EquipmentId.HasValue)
                {
                    ForEquipment      = true;
                    SelectedEquipment = AllEquipment.FirstOrDefault(e => e.Id == alert.EquipmentId);
                }
                else if (alert.CategoryId.HasValue)
                {
                    ForCategory      = true;
                    SelectedCategory = AllCategories.FirstOrDefault(c => c.Id == alert.CategoryId);
                }
                else
                {
                    ForGeneral = true;
                }
            }
            else
            {
                // Tryb dodawania
                Name             = string.Empty;
                Description      = null;
                Content          = string.Empty;
                TriggerDate      = DateTime.Today;
                ForEquipment     = true;
                SelectedEquipment = null;
                SelectedCategory = null;
                EquipmentSearch  = string.Empty;
            }
        }

        /// <summary>Inicjalizacja z pre-wybranym sprzętem (np. z menu kontekstowego w widoku sprzętu).</summary>
        public async Task InitializeForEquipmentAsync(Equipment equipment)
        {
            await InitializeAsync();
            ForEquipment      = true;
            SelectedEquipment = AllEquipment.FirstOrDefault(e => e.Id == equipment.Id);
        }

        // ─── Filtrowanie sprzętu ──────────────────────────────────────────────

        private void FilterEquipment()
        {
            FilteredEquipment.Clear();

            IEnumerable<Equipment> query = AllEquipment;
            if (!string.IsNullOrWhiteSpace(EquipmentSearch))
            {
                query = query.Where(e =>
                    (e.InventoryNumber?.Contains(EquipmentSearch, StringComparison.OrdinalIgnoreCase) == true) ||
                    (e.Name.Contains(EquipmentSearch, StringComparison.OrdinalIgnoreCase)) ||
                    (e.Location?.Name?.Contains(EquipmentSearch, StringComparison.OrdinalIgnoreCase) == true));
            }

            foreach (var e in query) FilteredEquipment.Add(e);
        }

        // ─── Komendy ──────────────────────────────────────────────────────────

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                System.Windows.MessageBox.Show(
                    "Nazwa powiadomienia jest wymagana.",
                    "Walidacja", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Content))
            {
                System.Windows.MessageBox.Show(
                    "Treść powiadomienia jest wymagana.",
                    "Walidacja", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (ForEquipment && SelectedEquipment == null)
            {
                System.Windows.MessageBox.Show(
                    "Wybierz sprzęt lub zmień zakres na 'Kategoria' albo 'Ogólne'.",
                    "Walidacja", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (ForCategory && SelectedCategory == null)
            {
                System.Windows.MessageBox.Show(
                    "Wybierz kategorię lub zmień zakres na 'Sprzęt' albo 'Ogólne'.",
                    "Walidacja", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                var alert = _editedAlert ?? new Alert();
                alert.Name        = Name.Trim();
                alert.Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
                alert.Content     = Content.Trim();
                alert.TriggerDate = TriggerDate;
                alert.EquipmentId = ForEquipment ? SelectedEquipment?.Id : null;
                alert.CategoryId  = ForCategory  ? SelectedCategory?.Id  : null;

                if (_isEditMode)
                    await _alertService.UpdateAsync(alert);
                else
                    await _alertService.CreateAsync(alert);

                OnSaveCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Błąd podczas zapisywania powiadomienia:\n{ex.Message}",
                    "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Cancel() => OnCancelRequested?.Invoke();
    }
}
