using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InwentaryzacjaSprzetu.Database;
using InwentaryzacjaSprzetu.Models;

namespace InwentaryzacjaSprzetu.Services
{
    public interface IDepartmentService
    {
        Task<List<Department>> GetAllAsync();
        Task<Department?> GetByIdAsync(int id);
        Task<Department> CreateAsync(Department department);
        Task<Department> UpdateAsync(Department department);
        Task DeleteAsync(int id);
        Task<List<Department>> SearchAsync(string searchTerm);
    }

    public class DepartmentService : IDepartmentService
    {
        private readonly InventoryDbContext _context;

        public DepartmentService(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<List<Department>> GetAllAsync()
        {
            return await _context.Departments
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<Department?> GetByIdAsync(int id)
        {
            return await _context.Departments.FindAsync(id);
        }

        public async Task<Department> CreateAsync(Department department)
        {
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return department;
        }

        public async Task<Department> UpdateAsync(Department department)
        {
            _context.Entry(department).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return department;
        }

        public async Task DeleteAsync(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department != null)
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Department>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllAsync();
            }

            return await _context.Departments
                .Where(d => d.Name.Contains(searchTerm) ||
                           d.Code.Contains(searchTerm) ||
                           (d.Description != null && d.Description.Contains(searchTerm)))
                .OrderBy(d => d.Name)
                .ToListAsync();
        }
    }
}