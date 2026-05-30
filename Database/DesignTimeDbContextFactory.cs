using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InwentaryzacjaSprzetu.Database
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
    {
        public InventoryDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "inventory.db");
            var connectionString = $"Data Source={dbPath}";
            
            Console.WriteLine($"DesignTimeDbContextFactory: Creating DbContext with connection string: {connectionString}");
            optionsBuilder.UseSqlite(connectionString);

            return new InventoryDbContext(optionsBuilder.Options);
        }
    }
}