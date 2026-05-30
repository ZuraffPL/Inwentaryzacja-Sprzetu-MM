using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using InwentaryzacjaSprzetu.Models;
using InwentaryzacjaSprzetu.Services;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class EquipmentEventsWindow : Window
    {
        private readonly Equipment _equipment;
        private readonly IInventoryEventService _eventService;

        public ObservableCollection<InventoryEvent> Events { get; } = new();

        public EquipmentEventsWindow(Equipment equipment, IInventoryEventService eventService)
        {
            InitializeComponent();
            DataContext = this;
            _equipment = equipment;
            _eventService = eventService;

            // Nagłówek okna z danymi sprzętu
            Title = $"Zdarzenia: {equipment.InventoryNumber}";
            TitleText.Text = $"{equipment.InventoryNumber} – {equipment.Name}";
            SubtitleText.Text = equipment.Category != null
                ? $"Kategoria: {equipment.Category.Name}  |  Lokalizacja: {equipment.Location?.Name ?? "—"}"
                : $"Lokalizacja: {equipment.Location?.Name ?? "—"}";

            Loaded += async (s, e) => await LoadEventsAsync();
        }

        private async Task LoadEventsAsync()
        {
            try
            {
                StatusText.Text = "Ładowanie…";
                var events = await _eventService.GetByEquipmentIdAsync(_equipment.Id);
                Events.Clear();
                foreach (var ev in events.OrderByDescending(x => x.EventDate))
                    Events.Add(ev);
                StatusText.Text = $"Łącznie: {Events.Count} zdarzeń";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Błąd ładowania: {ex.Message}";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
