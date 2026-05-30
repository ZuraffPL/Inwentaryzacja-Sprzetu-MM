using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using InwentaryzacjaSprzetu.Models;
using InwentaryzacjaSprzetu.Services;

namespace InwentaryzacjaSprzetu.ViewModels
{
    public partial class EventAttachmentsViewModel : ObservableObject
    {
        private readonly IEventAttachmentService _attachmentService;
        private int _eventId;

        public string EventDescription { get; private set; } = string.Empty;

        public ObservableCollection<EventAttachment> Attachments { get; } = new();

        [ObservableProperty]
        private EventAttachment? _selectedAttachment;

        public EventAttachmentsViewModel(IEventAttachmentService attachmentService)
        {
            _attachmentService = attachmentService;
        }

        public async Task InitializeAsync(InventoryEvent inventoryEvent)
        {
            _eventId = inventoryEvent.Id;
            EventDescription = $"{inventoryEvent.EventDate:dd.MM.yyyy} — {inventoryEvent.Description}";
            OnPropertyChanged(nameof(EventDescription));
            await LoadAttachmentsAsync();
        }

        private async Task LoadAttachmentsAsync()
        {
            Attachments.Clear();
            var items = await _attachmentService.GetByEventIdAsync(_eventId);
            foreach (var item in items)
                Attachments.Add(item);
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

            var result = await _attachmentService.AddAsync(_eventId, dialog.FileName);
            if (result != null)
                await LoadAttachmentsAsync();
        }

        [RelayCommand]
        private void OpenAttachment(EventAttachment? attachment)
        {
            if (attachment == null) return;

            var path = _attachmentService.GetFullPath(attachment);
            if (path == null || !File.Exists(path))
            {
                MessageBox.Show("Plik nie został znaleziony. Możliwe że folder załączników uległ zmianie lub plik został usunięty.",
                    "Brak pliku", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nie można otworzyć pliku:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task DeleteAttachmentAsync(EventAttachment? attachment)
        {
            if (attachment == null) return;

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć załącznik:\n{attachment.OriginalFileName}?\n\nPlik zostanie trwale usunięty z dysku.",
                "Usuń załącznik", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            await _attachmentService.DeleteAsync(attachment.Id);
            await LoadAttachmentsAsync();
        }

        [RelayCommand]
        private void ChangeAttachmentsFolder()
        {
            _attachmentService.PromptAndSetAttachmentsFolder();
        }
    }
}
