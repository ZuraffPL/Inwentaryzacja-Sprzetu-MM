using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwentaryzacjaSprzetu.Models;
using InwentaryzacjaSprzetu.Services;

namespace InwentaryzacjaSprzetu.ViewModels
{
    public partial class EventsViewModel : ObservableObject
    {
        private readonly IInventoryEventService _eventService;

        [ObservableProperty]
        private InventoryEvent? _selectedEvent;

        public ObservableCollection<InventoryEvent> Events { get; } = new();

        public EventsViewModel(IInventoryEventService eventService)
        {
            _eventService = eventService;
        }

        public async Task InitializeAsync()
        {
            await LoadEventsAsync();
        }

        [RelayCommand]
        private async Task LoadEventsAsync()
        {
            try
            {
                var events = await _eventService.GetAllAsync();
                Events.Clear();
                foreach (var eventItem in events)
                {
                    Events.Add(eventItem);
                }
            }
            catch (Exception)
            {
                Events.Clear();
            }
        }

        [RelayCommand]
        private async Task AddEventAsync()
        {
            // TODO: Implement dialog for adding new event
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task EditEventAsync()
        {
            if (SelectedEvent == null) return;
            
            // TODO: Implement dialog for editing event
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task DeleteEventAsync()
        {
            if (SelectedEvent == null) return;

            try
            {
                await _eventService.DeleteAsync(SelectedEvent.Id);
                await LoadEventsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas usuwania zdarzenia: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}