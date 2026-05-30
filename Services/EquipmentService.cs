using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InwentaryzacjaSprzetu.Database;
using InwentaryzacjaSprzetu.Models;

namespace InwentaryzacjaSprzetu.Services
{
    public class EquipmentService : IEquipmentService
    {
        private readonly InventoryDbContext _context;

        public EquipmentService(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Equipment>> GetAllAsync()
        {
            return await _context.Equipment
                .Include(e => e.Category)
                .Include(e => e.Location)
                .Include(e => e.Department)
                .Include(e => e.ConnectedToEquipment)
                .Include(e => e.InventoryEvents)
                .OrderBy(e => e.Category.SortOrder)
                .ThenBy(e => e.InventoryNumber)
                .ToListAsync();
        }

        public async Task<Equipment?> GetByIdAsync(int id)
        {
            return await _context.Equipment
                .Include(e => e.Category)
                .Include(e => e.Location)
                .Include(e => e.Department)
                .Include(e => e.ConnectedToEquipment)
                .Include(e => e.InventoryEvents)
                .Include(e => e.ChangeLogs)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Equipment?> GetByInventoryNumberAsync(string inventoryNumber)
        {
            return await _context.Equipment
                .Include(e => e.Category)
                .Include(e => e.Location)
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.InventoryNumber == inventoryNumber);
        }

        public async Task<Equipment> CreateAsync(Equipment equipment)
        {
            equipment.CreatedDate = DateTime.Now;
            _context.Equipment.Add(equipment);
            await _context.SaveChangesAsync();
            
            // Załaduj powiązane dane
            await _context.Entry(equipment)
                .Reference(e => e.Category)
                .LoadAsync();
            await _context.Entry(equipment)
                .Reference(e => e.Location)
                .LoadAsync();
            await _context.Entry(equipment)
                .Reference(e => e.Department)
                .LoadAsync();

            return equipment;
        }

        public async Task<Equipment> AddAsync(Equipment equipment)
        {
            return await CreateAsync(equipment);
        }

        public async Task<Equipment> UpdateAsync(Equipment equipment)
        {
            var existingEquipment = await _context.Equipment.FirstOrDefaultAsync(e => e.Id == equipment.Id);
            if (existingEquipment == null)
                throw new ArgumentException("Sprzęt nie został znaleziony");

            // Zapisz zmiany w ChangeLog
            LogChanges(existingEquipment, equipment);

            // Aktualizuj właściwości
            _context.Entry(existingEquipment).CurrentValues.SetValues(equipment);
            existingEquipment.LastModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            // Załaduj powiązane dane
            await _context.Entry(existingEquipment)
                .Reference(e => e.Category)
                .LoadAsync();
            await _context.Entry(existingEquipment)
                .Reference(e => e.Location)
                .LoadAsync();

            return existingEquipment;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var equipment = await _context.Equipment.FindAsync(id);
            if (equipment == null)
                return false;

            _context.Equipment.Remove(equipment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Equipment>> GetByLocationAsync(int locationId)
        {
            return await _context.Equipment
                .Include(e => e.Category)
                .Include(e => e.Location)
                .Include(e => e.Department)
                .Include(e => e.ConnectedToEquipment)
                .Where(e => e.LocationId == locationId)
                .OrderBy(e => e.Category.SortOrder)
                .ThenBy(e => e.InventoryNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Equipment>> GetByCategoryAsync(int categoryId)
        {
            return await _context.Equipment
                .Include(e => e.Category)
                .Include(e => e.Location)
                .Include(e => e.Department)
                .Where(e => e.CategoryId == categoryId)
                .OrderBy(e => e.InventoryNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Equipment>> GetConnectedToAsync(int computerId)
        {
            return await _context.Equipment
                .Include(e => e.Category)
                .Include(e => e.Location)
                .Include(e => e.Department)
                .Where(e => e.ConnectedToEquipmentId == computerId)
                .OrderBy(e => e.Category != null ? e.Category.SortOrder : 0)
                .ThenBy(e => e.InventoryNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Equipment>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            searchTerm = searchTerm.ToLower();

            return await _context.Equipment
                .Include(e => e.Category)
                .Include(e => e.Location)
                .Include(e => e.Department)
                .Where(e => e.InventoryNumber.ToLower().Contains(searchTerm) ||
                           e.Name.ToLower().Contains(searchTerm) ||
                           (e.Brand != null && e.Brand.ToLower().Contains(searchTerm)) ||
                           (e.Model != null && e.Model.ToLower().Contains(searchTerm)) ||
                           (e.SerialNumber != null && e.SerialNumber.ToLower().Contains(searchTerm)) ||
                           (e.IpAddress != null && e.IpAddress.ToLower().Contains(searchTerm)) ||
                           (e.LocationDetails != null && e.LocationDetails.ToLower().Contains(searchTerm)) ||
                           (e.Description != null && e.Description.ToLower().Contains(searchTerm)) ||
                           (e.Notes != null && e.Notes.ToLower().Contains(searchTerm)) ||
                           e.Category.Name.ToLower().Contains(searchTerm) ||
                           e.Location.Name.ToLower().Contains(searchTerm) ||
                           e.Status.ToString().ToLower().Contains(searchTerm) ||
                           (e.Department != null && e.Department.Name.ToLower().Contains(searchTerm)))
                .OrderBy(e => e.InventoryNumber)
                .ToListAsync();
        }

        public async Task<string> GenerateInventoryNumberAsync(int categoryId, int locationId)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            var location = await _context.Locations.FindAsync(locationId);

            if (category == null || location == null)
                throw new ArgumentException("Kategoria lub lokalizacja nie została znaleziona");

            var prefix = $"{category.Code}-{location.Code}-";
            var year = DateTime.Now.Year.ToString();
            
            var lastNumber = await _context.Equipment
                .Where(e => e.InventoryNumber.StartsWith(prefix + year))
                .OrderByDescending(e => e.InventoryNumber)
                .Select(e => e.InventoryNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastNumber))
            {
                var numberPart = lastNumber.Substring((prefix + year).Length);
                if (int.TryParse(numberPart, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            return $"{prefix}{year}{nextNumber:D3}";
        }

        private void LogChanges(Equipment oldEquipment, Equipment newEquipment)
        {
            var changes = new List<ChangeLog>();

            if (oldEquipment.Name != newEquipment.Name)
                changes.Add(new ChangeLog { EquipmentId = oldEquipment.Id, FieldName = "Name", OldValue = oldEquipment.Name, NewValue = newEquipment.Name });

            if (oldEquipment.Description != newEquipment.Description)
                changes.Add(new ChangeLog { EquipmentId = oldEquipment.Id, FieldName = "Description", OldValue = oldEquipment.Description, NewValue = newEquipment.Description });

            if (oldEquipment.Brand != newEquipment.Brand)
                changes.Add(new ChangeLog { EquipmentId = oldEquipment.Id, FieldName = "Brand", OldValue = oldEquipment.Brand, NewValue = newEquipment.Brand });

            if (oldEquipment.Model != newEquipment.Model)
                changes.Add(new ChangeLog { EquipmentId = oldEquipment.Id, FieldName = "Model", OldValue = oldEquipment.Model, NewValue = newEquipment.Model });

            if (oldEquipment.SerialNumber != newEquipment.SerialNumber)
                changes.Add(new ChangeLog { EquipmentId = oldEquipment.Id, FieldName = "SerialNumber", OldValue = oldEquipment.SerialNumber, NewValue = newEquipment.SerialNumber });

            if (oldEquipment.Status != newEquipment.Status)
                changes.Add(new ChangeLog { EquipmentId = oldEquipment.Id, FieldName = "Status", OldValue = oldEquipment.Status.ToString(), NewValue = newEquipment.Status.ToString() });

            if (oldEquipment.LocationId != newEquipment.LocationId)
                changes.Add(new ChangeLog { EquipmentId = oldEquipment.Id, FieldName = "LocationId", OldValue = oldEquipment.LocationId.ToString(), NewValue = newEquipment.LocationId.ToString() });

            if (oldEquipment.CategoryId != newEquipment.CategoryId)
                changes.Add(new ChangeLog { EquipmentId = oldEquipment.Id, FieldName = "CategoryId", OldValue = oldEquipment.CategoryId.ToString(), NewValue = newEquipment.CategoryId.ToString() });

            if (changes.Any())
            {
                _context.ChangeLogs.AddRange(changes);
            }
        }
    }
}