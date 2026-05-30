using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using InwentaryzacjaSprzetu.Database;
using InwentaryzacjaSprzetu.Helpers;
using InwentaryzacjaSprzetu.Models;

namespace InwentaryzacjaSprzetu.Services
{
    public class EventAttachmentService : IEventAttachmentService
    {
        private readonly InventoryDbContext _context;

        public EventAttachmentService(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<EventAttachment>> GetByEventIdAsync(int eventId)
        {
            return await _context.EventAttachments
                .Where(a => a.EventId == eventId)
                .OrderBy(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<EventAttachment?> AddAsync(int eventId, string sourceFilePath)
        {
            var folder = EnsureFolder();
            if (folder == null) return null;

            var ext = Path.GetExtension(sourceFilePath).ToLowerInvariant();
            var storedName = $"{eventId}_{Guid.NewGuid():N}{ext}";
            var destPath = Path.Combine(folder, storedName);

            try
            {
                File.Copy(sourceFilePath, destPath, overwrite: false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Nie udało się skopiować pliku do folderu załączników:\n{ex.Message}",
                    "Błąd kopiowania pliku", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            var attachment = new EventAttachment
            {
                EventId          = eventId,
                OriginalFileName = Path.GetFileName(sourceFilePath),
                StoredFileName   = storedName,
                CreatedAt        = DateTime.Now
            };

            _context.EventAttachments.Add(attachment);
            await _context.SaveChangesAsync();
            return attachment;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var attachment = await _context.EventAttachments.FindAsync(id);
            if (attachment == null) return false;

            // Usuń plik z dysku (nie przerywaj jeśli plik nie istnieje)
            var fullPath = GetFullPath(attachment);
            if (fullPath != null && File.Exists(fullPath))
            {
                try { File.Delete(fullPath); }
                catch { /* Plik zablokowany lub brak dostępu — pomijamy, rekord i tak usuwamy */ }
            }

            _context.EventAttachments.Remove(attachment);
            await _context.SaveChangesAsync();
            return true;
        }

        public string? GetAttachmentsFolder()
        {
            var prefs = AppPreferences.Load();
            return string.IsNullOrWhiteSpace(prefs.AttachmentsFolder) ? null : prefs.AttachmentsFolder;
        }

        public string? PromptAndSetAttachmentsFolder()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description         = "Wybierz folder do przechowywania załączników zdarzeń",
                ShowNewFolderButton = true,
                UseDescriptionForTitle = true
            };

            // Ustaw domyślny folder startowy na poprzednio wybrany lub Dokumenty
            var current = GetAttachmentsFolder();
            if (current != null && Directory.Exists(current))
                dialog.InitialDirectory = current;
            else
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return null;

            var selected = dialog.SelectedPath;

            try
            {
                Directory.CreateDirectory(selected);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Nie można utworzyć folderu:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            var prefs = AppPreferences.Load();
            prefs.AttachmentsFolder = selected;
            prefs.Save();

            return selected;
        }

        public string? GetFullPath(EventAttachment attachment)
        {
            var folder = GetAttachmentsFolder();
            if (folder == null) return null;
            return Path.Combine(folder, attachment.StoredFileName);
        }

        // ── Prywatne ──────────────────────────────────────────────────────────────

        /// <summary>Zwraca folder załączników. Jeśli niezdefiniowany — pyta użytkownika. Null = anulowano.</summary>
        private string? EnsureFolder()
        {
            var folder = GetAttachmentsFolder();
            if (folder != null && Directory.Exists(folder))
                return folder;

            // Folder niezdefiniowany lub usunięty — zapytaj
            MessageBox.Show(
                "Nie wybrano jeszcze folderu przechowywania załączników.\n\nW następnym oknie wskaż folder, w którym będą zapisywane pliki.",
                "Folder załączników", MessageBoxButton.OK, MessageBoxImage.Information);

            return PromptAndSetAttachmentsFolder();
        }
    }
}
