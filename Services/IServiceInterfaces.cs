using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InwentaryzacjaSprzetu.Models;

namespace InwentaryzacjaSprzetu.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<Category> CreateAsync(Category category);
        Task<Category> AddAsync(Category category);
        Task<Category> UpdateAsync(Category category);
        Task<bool> DeleteAsync(int id);
        Task<bool> CanDeleteAsync(int id);
        Task<IEnumerable<Category>> SearchAsync(string searchTerm);
    }

    public interface ILocationService
    {
        Task<IEnumerable<Location>> GetAllAsync();                      // tylko aktywne (dropdowny)
        Task<IEnumerable<Location>> GetAllIncludingInactiveAsync();     // wszystkie (widok zarządzania)
        Task<Location?> GetByIdAsync(int id);
        Task<Location> CreateAsync(Location location);
        Task<Location> AddAsync(Location location);
        Task<Location> UpdateAsync(Location location);
        Task<bool> DeleteAsync(int id);
        Task<bool> CanDeleteAsync(int id);
        Task<IEnumerable<Location>> SearchAsync(string searchTerm);
        Task<IEnumerable<Location>> SearchAllAsync(string searchTerm); // wszystkie (widok zarządzania)
    }

    public interface IInventoryEventService
    {
        Task<IEnumerable<InventoryEvent>> GetAllAsync();
        Task<InventoryEvent?> GetByIdAsync(int id);
        Task<IEnumerable<InventoryEvent>> GetByEquipmentIdAsync(int equipmentId);
        Task<IEnumerable<InventoryEvent>> GetByLocationAsync(int locationId);
        Task<InventoryEvent> CreateAsync(InventoryEvent inventoryEvent);
        Task<InventoryEvent> UpdateAsync(InventoryEvent inventoryEvent);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<InventoryEvent>> SearchAsync(string searchTerm);
        /// <summary>Uzupełnia PreviousStatus w historycznych zdarzeniach StatusChange które mają null (dane sprzed v1.3.0)</summary>
        Task RepairHistoricalEventsAsync();
    }

    public interface IEventAttachmentService
    {
        Task<IEnumerable<EventAttachment>> GetByEventIdAsync(int eventId);
        /// <summary>Kopiuje plik do folderu załączników i zapisuje rekord w bazie. Zwraca null jeśli anulowano wybór folderu.</summary>
        Task<EventAttachment?> AddAsync(int eventId, string sourceFilePath);
        Task<bool> DeleteAsync(int id);
        /// <summary>Pełna ścieżka do folderu przechowywania załączników (null jeśli niezdefiniowany).</summary>
        string? GetAttachmentsFolder();
        /// <summary>Pyta użytkownika o folder i zapisuje go w preferencjach. Zwraca wybraną ścieżkę lub null.</summary>
        string? PromptAndSetAttachmentsFolder();
        /// <summary>Zwraca pełną ścieżkę do pliku załącznika lub null jeśli plik nie istnieje.</summary>
        string? GetFullPath(EventAttachment attachment);
    }
}