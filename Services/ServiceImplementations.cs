using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InwentaryzacjaSprzetu.Database;
using InwentaryzacjaSprzetu.Models;

namespace InwentaryzacjaSprzetu.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly InventoryDbContext _context;

        public CategoryService(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Categories
                .Include(c => c.Equipment)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Category> CreateAsync(Category category)
        {
            category.CreatedDate = DateTime.Now;
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Category> AddAsync(Category category)
        {
            return await CreateAsync(category);
        }

        public async Task<Category> UpdateAsync(Category category)
        {
            var existingCategory = await _context.Categories.FindAsync(category.Id);
            if (existingCategory == null)
                throw new ArgumentException("Kategoria nie została znaleziona");

            _context.Entry(existingCategory).CurrentValues.SetValues(category);
            await _context.SaveChangesAsync();
            return existingCategory;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return false;

            if (!await CanDeleteAsync(id))
                throw new InvalidOperationException("Nie można usunąć kategorii, która jest używana przez sprzęt");

            category.IsActive = false; // Soft delete
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CanDeleteAsync(int id)
        {
            return !await _context.Equipment.AnyAsync(e => e.CategoryId == id);
        }

        public async Task<IEnumerable<Category>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            var searchLower = searchTerm.ToLower();
            return await _context.Categories
                .Where(c => c.IsActive && (
                    c.Name.ToLower().Contains(searchLower) ||
                    (c.Code != null && c.Code.ToLower().Contains(searchLower)) ||
                    (c.Description != null && c.Description.ToLower().Contains(searchLower))
                ))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }

    public class LocationService : ILocationService
    {
        private readonly InventoryDbContext _context;

        public LocationService(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Location>> GetAllAsync()
        {
            return await _context.Locations
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetAllIncludingInactiveAsync()
        {
            return await _context.Locations
                .OrderBy(l => l.IsActive ? 0 : 1)  // aktywne najpierw
                .ThenBy(l => l.Name)
                .ToListAsync();
        }

        public async Task<Location?> GetByIdAsync(int id)
        {
            return await _context.Locations
                .Include(l => l.Equipment)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<Location> CreateAsync(Location location)
        {
            location.CreatedDate = DateTime.Now;
            _context.Locations.Add(location);
            await _context.SaveChangesAsync();
            return location;
        }

        public async Task<Location> AddAsync(Location location)
        {
            return await CreateAsync(location);
        }

        public async Task<Location> UpdateAsync(Location location)
        {
            var existingLocation = await _context.Locations.FindAsync(location.Id);
            if (existingLocation == null)
                throw new ArgumentException("Lokalizacja nie została znaleziona");

            _context.Entry(existingLocation).CurrentValues.SetValues(location);
            await _context.SaveChangesAsync();
            return existingLocation;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location == null)
                return false;

            if (!await CanDeleteAsync(id))
                throw new InvalidOperationException("Nie można usunąć lokalizacji, która jest używana przez sprzęt");

            location.IsActive = false; // Soft delete
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CanDeleteAsync(int id)
        {
            return !await _context.Equipment.AnyAsync(e => e.LocationId == id);
        }

        public async Task<IEnumerable<Location>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            var searchLower = searchTerm.ToLower();
            return await _context.Locations
                .Where(l => l.IsActive && (
                    l.Name.ToLower().Contains(searchLower) ||
                    (l.Code != null && l.Code.ToLower().Contains(searchLower)) ||
                    (l.Address != null && l.Address.ToLower().Contains(searchLower)) ||
                    (l.City != null && l.City.ToLower().Contains(searchLower)) ||
                    (l.Description != null && l.Description.ToLower().Contains(searchLower))
                ))
                .OrderBy(l => l.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> SearchAllAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllIncludingInactiveAsync();

            var searchLower = searchTerm.ToLower();
            return await _context.Locations
                .Where(l =>
                    l.Name.ToLower().Contains(searchLower) ||
                    (l.Code != null && l.Code.ToLower().Contains(searchLower)) ||
                    (l.Address != null && l.Address.ToLower().Contains(searchLower)) ||
                    (l.City != null && l.City.ToLower().Contains(searchLower)) ||
                    (l.Description != null && l.Description.ToLower().Contains(searchLower))
                )
                .OrderBy(l => l.IsActive ? 0 : 1)
                .ThenBy(l => l.Name)
                .ToListAsync();
        }
    }

    public class InventoryEventService : IInventoryEventService
    {
        private readonly InventoryDbContext _context;

        public InventoryEventService(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<InventoryEvent>> GetAllAsync()
        {
            return await _context.InventoryEvents
                .Include(ie => ie.Equipment)
                .Include(ie => ie.Location)
                .Include(ie => ie.Attachments)
                .OrderByDescending(ie => ie.EventDate)
                .ToListAsync();
        }

        public async Task<InventoryEvent?> GetByIdAsync(int id)
        {
            return await _context.InventoryEvents
                .Include(ie => ie.Equipment)
                .Include(ie => ie.Location)
                .FirstOrDefaultAsync(ie => ie.Id == id);
        }

        public async Task<IEnumerable<InventoryEvent>> GetByEquipmentIdAsync(int equipmentId)
        {
            return await _context.InventoryEvents
                .Include(ie => ie.Equipment)
                .Include(ie => ie.Location)
                .Include(ie => ie.Attachments)
                .Where(ie => ie.EquipmentId == equipmentId)
                .OrderByDescending(ie => ie.EventDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<InventoryEvent>> GetByLocationAsync(int locationId)
        {
            return await _context.InventoryEvents
                .Include(ie => ie.Equipment)
                .Include(ie => ie.Location)
                .Include(ie => ie.Attachments)
                .Where(ie => ie.LocationId == locationId)
                .OrderByDescending(ie => ie.EventDate)
                .ToListAsync();
        }

        public async Task<InventoryEvent> CreateAsync(InventoryEvent inventoryEvent)
        {
            inventoryEvent.CreatedDate = DateTime.Now;
            _context.InventoryEvents.Add(inventoryEvent);
            await _context.SaveChangesAsync();

            // Załaduj powiązane dane
            await _context.Entry(inventoryEvent)
                .Reference(ie => ie.Equipment)
                .LoadAsync();
            await _context.Entry(inventoryEvent)
                .Reference(ie => ie.Location)
                .LoadAsync();

            return inventoryEvent;
        }

        public async Task<InventoryEvent> UpdateAsync(InventoryEvent inventoryEvent)
        {
            var existingEvent = await _context.InventoryEvents.FindAsync(inventoryEvent.Id);
            if (existingEvent == null)
                throw new ArgumentException("Zdarzenie nie zostało znalezione");

            _context.Entry(existingEvent).CurrentValues.SetValues(inventoryEvent);
            await _context.SaveChangesAsync();

            await _context.Entry(existingEvent)
                .Reference(ie => ie.Equipment)
                .LoadAsync();
            await _context.Entry(existingEvent)
                .Reference(ie => ie.Location)
                .LoadAsync();

            return existingEvent;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var inventoryEvent = await _context.InventoryEvents.FindAsync(id);
            if (inventoryEvent == null)
                return false;

            _context.InventoryEvents.Remove(inventoryEvent);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<InventoryEvent>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            var searchLower = searchTerm.ToLower();
            return await _context.InventoryEvents
                .Include(ie => ie.Equipment)
                .Include(ie => ie.Location)
                .Include(ie => ie.Attachments)
                .Where(ie => 
                    (ie.Description != null && ie.Description.ToLower().Contains(searchLower)) ||
                    (ie.PerformedBy != null && ie.PerformedBy.ToLower().Contains(searchLower)) ||
                    (ie.Notes != null && ie.Notes.ToLower().Contains(searchLower)) ||
                    (ie.Equipment != null && ie.Equipment.Name.ToLower().Contains(searchLower)) ||
                    (ie.Equipment != null && ie.Equipment.InventoryNumber.ToLower().Contains(searchLower)) ||
                    (ie.Location != null && ie.Location.Name.ToLower().Contains(searchLower))
                )
                .OrderByDescending(ie => ie.EventDate)
                .ToListAsync();
        }

        public async Task RepairHistoricalEventsAsync()
        {
            // Pobierz wszystkie zdarzenia StatusChange powiązane ze sprzętem, posortowane po sprzęcie i dacie
            var events = await _context.InventoryEvents
                .Where(e => e.EventType == InventoryEventType.StatusChange && e.EquipmentId != null)
                .OrderBy(e => e.EquipmentId)
                .ThenBy(e => e.EventDate)
                .ToListAsync();

            bool changed = false;
            InventoryEvent? previous = null;

            foreach (var ev in events)
            {
                if (previous != null
                    && previous.EquipmentId == ev.EquipmentId
                    && ev.PreviousStatus == null
                    && previous.NewStatus != null)
                {
                    ev.PreviousStatus = previous.NewStatus;
                    changed = true;
                }
                previous = ev;
            }

            if (changed)
                await _context.SaveChangesAsync();
        }
    }
}