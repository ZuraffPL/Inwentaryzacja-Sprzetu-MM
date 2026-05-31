using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InwentaryzacjaSprzetu.Database;
using InwentaryzacjaSprzetu.Models;

namespace InwentaryzacjaSprzetu.Services
{
    public interface IAlertService
    {
        /// <summary>Wszystkie niezarchiwizowane alerty (aktywne i zaplanowane).</summary>
        Task<List<Alert>> GetActiveAlertsAsync();

        /// <summary>Zarchiwizowane alerty.</summary>
        Task<List<Alert>> GetArchivedAlertsAsync();

        Task<Alert?> GetByIdAsync(int id);
        Task<Alert> CreateAsync(Alert alert);
        Task<Alert> UpdateAsync(Alert alert);
        Task<bool> DeleteAsync(int id);
        Task<bool> ArchiveAsync(int id);

        /// <summary>Liczba niezarchiwizowanych alertów z TriggerDate &lt;= dziś.</summary>
        Task<int> GetTriggeredCountAsync();
    }

    public class AlertService : IAlertService
    {
        private readonly InventoryDbContext _context;

        public AlertService(InventoryDbContext context)
        {
            _context = context;
        }

        private IQueryable<Alert> WithIncludes() =>
            _context.Alerts
                .Include(a => a.Equipment).ThenInclude(e => e!.Location)
                .Include(a => a.Category);

        public async Task<List<Alert>> GetActiveAlertsAsync() =>
            await WithIncludes()
                .Where(a => !a.IsArchived)
                .OrderBy(a => a.TriggerDate)
                .ThenBy(a => a.Name)
                .ToListAsync();

        public async Task<List<Alert>> GetArchivedAlertsAsync() =>
            await WithIncludes()
                .Where(a => a.IsArchived)
                .OrderByDescending(a => a.ArchivedDate)
                .ToListAsync();

        public async Task<Alert?> GetByIdAsync(int id) =>
            await WithIncludes().FirstOrDefaultAsync(a => a.Id == id);

        public async Task<Alert> CreateAsync(Alert alert)
        {
            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(alert.Id))!;
        }

        public async Task<Alert> UpdateAsync(Alert alert)
        {
            var existing = await _context.Alerts.FindAsync(alert.Id)
                ?? throw new InvalidOperationException($"Alert o Id={alert.Id} nie istnieje.");

            existing.Name        = alert.Name;
            existing.Description = alert.Description;
            existing.Content     = alert.Content;
            existing.TriggerDate = alert.TriggerDate;
            existing.EquipmentId = alert.EquipmentId;
            existing.CategoryId  = alert.CategoryId;

            await _context.SaveChangesAsync();
            return (await GetByIdAsync(alert.Id))!;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var alert = await _context.Alerts.FindAsync(id);
            if (alert == null) return false;
            _context.Alerts.Remove(alert);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ArchiveAsync(int id)
        {
            var alert = await _context.Alerts.FindAsync(id);
            if (alert == null) return false;
            alert.IsArchived  = true;
            alert.ArchivedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetTriggeredCountAsync() =>
            await _context.Alerts.CountAsync(
                a => !a.IsArchived && a.TriggerDate.Date <= DateTime.Today);
    }
}
