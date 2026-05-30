using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using InwentaryzacjaSprzetu.Models;
using InwentaryzacjaSprzetu.Services;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class ConnectedEquipmentWindow : Window
    {
        private readonly Equipment _computer;
        private readonly IEquipmentService _equipmentService;

        public ObservableCollection<Equipment> Items { get; } = new();

        public ConnectedEquipmentWindow(Equipment computer, IEquipmentService equipmentService)
        {
            InitializeComponent();
            DataContext = this;
            _computer = computer;
            _equipmentService = equipmentService;

            Title = $"Sprzęty podpięte do: {computer.InventoryNumber}";
            TitleText.Text = $"🖥️  {computer.InventoryNumber} – {computer.Name}";
            SubtitleText.Text = computer.Category != null
                ? $"Kategoria: {computer.Category.Name}  |  Lokalizacja: {computer.Location?.Name ?? "—"}"
                : $"Lokalizacja: {computer.Location?.Name ?? "—"}";

            Loaded += async (s, e) => await LoadItemsAsync();
        }

        private async Task LoadItemsAsync()
        {
            try
            {
                StatusText.Text = "Ładowanie…";
                var items = await _equipmentService.GetConnectedToAsync(_computer.Id);
                Items.Clear();
                foreach (var item in items)
                    Items.Add(item);

                if (Items.Count == 0)
                    StatusText.Text = "Brak podpiętych urządzeń.";
                else
                    StatusText.Text = $"Łącznie: {Items.Count} urządzeń w {Items.Select(i => i.CategoryId).Distinct().Count()} kategoriach.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Błąd: {ex.Message}";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
