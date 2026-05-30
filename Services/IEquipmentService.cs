using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InwentaryzacjaSprzetu.Models;

namespace InwentaryzacjaSprzetu.Services
{
    public interface IEquipmentService
    {
        Task<IEnumerable<Equipment>> GetAllAsync();
        Task<Equipment?> GetByIdAsync(int id);
        Task<Equipment?> GetByInventoryNumberAsync(string inventoryNumber);
        Task<Equipment> CreateAsync(Equipment equipment);
        Task<Equipment> AddAsync(Equipment equipment);
        Task<Equipment> UpdateAsync(Equipment equipment);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<Equipment>> GetByLocationAsync(int locationId);
        Task<IEnumerable<Equipment>> GetByCategoryAsync(int categoryId);
        Task<IEnumerable<Equipment>> GetConnectedToAsync(int computerId);
        Task<IEnumerable<Equipment>> SearchAsync(string searchTerm);
        Task<string> GenerateInventoryNumberAsync(int categoryId, int locationId);
    }
}