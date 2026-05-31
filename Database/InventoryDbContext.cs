using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InwentaryzacjaSprzetu.Models;

namespace InwentaryzacjaSprzetu.Database
{
    public class InventoryDbContext : DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
        {
        }

        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<InventoryEvent> InventoryEvents { get; set; }
        public DbSet<ChangeLog> ChangeLogs { get; set; }
        public DbSet<EventAttachment> EventAttachments { get; set; }
        public DbSet<Alert> Alerts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Konfiguracja Equipment
            modelBuilder.Entity<Equipment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.InventoryNumber).IsRequired();
                entity.HasIndex(e => e.InventoryNumber).IsUnique();
                
                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Equipment)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Location)
                    .WithMany(l => l.Equipment)
                    .HasForeignKey(e => e.LocationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Department)
                    .WithMany(d => d.Equipment)
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.ConnectedToEquipment)
                    .WithMany()
                    .HasForeignKey(e => e.ConnectedToEquipmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Konfiguracja Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired();
                entity.HasIndex(c => c.Code).IsUnique();
            });

            // Konfiguracja Location
            modelBuilder.Entity<Location>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.Property(l => l.Name).IsRequired();
                entity.HasIndex(l => l.Code).IsUnique();
            });

            // Konfiguracja Department
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Name).IsRequired();
                entity.HasIndex(d => d.Code).IsUnique();
            });

            // Konfiguracja InventoryEvent
            modelBuilder.Entity<InventoryEvent>(entity =>
            {
                entity.HasKey(ie => ie.Id);
                
                entity.HasOne(ie => ie.Equipment)
                    .WithMany(e => e.InventoryEvents)
                    .HasForeignKey(ie => ie.EquipmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ie => ie.Location)
                    .WithMany(l => l.InventoryEvents)
                    .HasForeignKey(ie => ie.LocationId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Konfiguracja EventAttachment
            modelBuilder.Entity<EventAttachment>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.ToTable("event_attachments");
                entity.Property(a => a.OriginalFileName).IsRequired();
                entity.Property(a => a.StoredFileName).IsRequired();

                entity.HasOne(a => a.Event)
                    .WithMany(e => e.Attachments)
                    .HasForeignKey(a => a.EventId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Konfiguracja Alert
            modelBuilder.Entity<Alert>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.ToTable("alerts");
                entity.Property(a => a.Name).IsRequired();
                entity.Property(a => a.Content).IsRequired();

                entity.HasOne(a => a.Equipment)
                    .WithMany()
                    .HasForeignKey(a => a.EquipmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(a => a.Category)
                    .WithMany()
                    .HasForeignKey(a => a.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Konfiguracja ChangeLog
            modelBuilder.Entity<ChangeLog>(entity =>
            {
                entity.HasKey(cl => cl.Id);
                
                entity.HasOne(cl => cl.Equipment)
                    .WithMany(e => e.ChangeLogs)
                    .HasForeignKey(cl => cl.EquipmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Dane początkowe będą dodawane przez osobną metodę po migracji
            // SeedData(modelBuilder); - usunięte aby nie powodować konfliktów z migracjami
        }

        // Dane początkowe - Twoje domyślne kategorie i lokalizacje
        public async Task SeedInitialDataAsync()
        {
            // Sprawdź i dodaj kategorie jeśli nie istnieją
            if (!await Categories.AnyAsync())
            {
                // Twoje kategorie - zgodnie z nową kolejnością sortowania
                var categories = new[]
                {
                    new Category { Code = "K",  Name = "Komputery",                Description = "Komputery stacjonarne i laptopy",              SortOrder = 1  },
                    new Category { Code = "M",  Name = "Monitory",                 Description = "Monitory LCD, LED i inne wyświetlacze",         SortOrder = 2  },
                    new Category { Code = "D",  Name = "Drukarki",                 Description = "Drukarki laserowe, atramentowe i wielofunkcyjne", SortOrder = 3 },
                    new Category { Code = "S",  Name = "Serwery",                  Description = "Serwery i urządzenia serwerowe",                SortOrder = 4  },
                    new Category { Code = "U",  Name = "UPSy",                     Description = "Zasilacze awaryjne UPS",                        SortOrder = 5  },
                    new Category { Code = "SR", Name = "Skanery",                  Description = "Skanery dokumentów i obrazów",                  SortOrder = 6  },
                    new Category { Code = "KS", Name = "Ksero",                    Description = "Kserokopiarki i urządzenia wielofunkcyjne",     SortOrder = 7  },
                    new Category { Code = "VI", Name = "Voice IP",                 Description = "Telefony i urządzenia VoIP",                    SortOrder = 8  },
                    new Category { Code = "AP", Name = "Router/Switch",            Description = "Routery, switche i urządzenia sieciowe",        SortOrder = 9  },
                    new Category { Code = "KK", Name = "Czytniki kodów kreskowych",Description = "Czytniki kodów kreskowych i QR",                SortOrder = 10 },
                    new Category { Code = "TE", Name = "Testery Banknotów",        Description = "Testery i detektory banknotów",                 SortOrder = 11 },
                    new Category { Code = "SC", Name = "Sprawdzarki cen",          Description = "Sprawdzarki i kontrolery cen",                  SortOrder = 12 },
                    new Category { Code = "KD", Name = "Kolektory Danych",         Description = "Kolektory danych i terminale",                  SortOrder = 13 },
                    new Category { Code = "TV", Name = "Telewizory",               Description = "Telewizory i wyświetlacze",                     SortOrder = 14 },
                    new Category { Code = "W",  Name = "Wagi",                     Description = "Wagi elektroniczne i systemy ważące",           SortOrder = 15 },
                    new Category { Code = "TK", Name = "Telefony komórkowe",       Description = "Telefony komórkowe i smartfony",                SortOrder = 16 },
                    new Category { Code = "L",  Name = "Laminatory",               Description = "Laminatory i urządzenia do laminowania",        SortOrder = 17 },
                    new Category { Code = "ND", Name = "Niszczarki",               Description = "Niszczarki dokumentów",                         SortOrder = 18 },
                    new Category { Code = "F",  Name = "Kasy Fiskalne",            Description = "Kasy fiskalne i drukarki fiskalne",             SortOrder = 19 },
                    new Category { Code = "LAP", Name = "Laptopy",                   Description = "Laptopy i komputery przenośne",                  SortOrder = 100 },
                };

                Categories.AddRange(categories);
                await SaveChangesAsync();
            }

            // Sprawdź i dodaj lokalizacje jeśli nie istnieją
            if (!await Locations.AnyAsync())
            {
                // Twoje lokalizacje
                var locations = new[]
                {
                    new Location { Code = "86", Name = "Merkury Zamość", Address = "", City = "Zamość", PostalCode = "", Description = "Sklep Merkury w Zamościu" },
                    new Location { Code = "99", Name = "Merkury Krosno", Address = "", City = "Krosno", PostalCode = "", Description = "Sklep Merkury w Krośnie" }
                };

                Locations.AddRange(locations);
                await SaveChangesAsync();
            }

            // Sprawdź i dodaj działy jeśli nie istnieją
            if (!await Departments.AnyAsync())
            {
                // Twoje działy
                var departments = new[]
                {
                    new Department { Code = "AGD", Name = "AGD", Description = "Dział AGD" },
                    new Department { Code = "CER", Name = "Ceramika", Description = "Dział ceramiki" },
                    new Department { Code = "CHE", Name = "Chemia", Description = "Dział chemii" },
                    new Department { Code = "DYW", Name = "Dywany", Description = "Dział dywan" },
                    new Department { Code = "ELE", Name = "Elektryka", Description = "Dział elektryki" },
                    new Department { Code = "FIR", Name = "Firany", Description = "Dział firan" },
                    new Department { Code = "KAS", Name = "Kasa", Description = "Kasa" },
                    new Department { Code = "KAR", Name = "Karnisze", Description = "Dział karniszy" },
                    new Department { Code = "KIE", Name = "Kierownik", Description = "Kierownictwo" },
                    new Department { Code = "LAM", Name = "Lampy", Description = "Dział lamp" },
                    new Department { Code = "MAG", Name = "Magazyn Główny", Description = "Magazyn główny" },
                    new Department { Code = "MEB", Name = "Magazyn Mebli", Description = "Magazyn mebli" },
                    new Department { Code = "ME", Name = "Meble", Description = "Dział mebli" },
                    new Department { Code = "OGR", Name = "Ogród", Description = "Dział ogrodowy" },
                    new Department { Code = "PLY", Name = "Płytka", Description = "Dział płytek" },
                    new Department { Code = "REZ", Name = "Rezerwa", Description = "Rezerwa" },
                    new Department { Code = "STO", Name = "Stolarka", Description = "Dział stolarki" },
                    new Department { Code = "SZA", Name = "Szafki łazienkowe", Description = "Dział szafek łazienkowych" },
                    new Department { Code = "WPR", Name = "Wprowadzanie", Description = "Wprowadzanie danych" }
                };

                Departments.AddRange(departments);
                await SaveChangesAsync();
            }

            // Jeśli nie ma sprzętu, dodaj przykładowy sprzęt
            if (!await Equipment.AnyAsync())
            {
                await SeedSampleEquipmentAsync();
            }

            // Upewnij się, że kategoria Laptopy istnieje (dodana w późniejszej wersji)
            if (!await Categories.AnyAsync(c => c.Code == "LAP"))
            {
                Categories.Add(new Category { Code = "LAP", Name = "Laptopy", Description = "Laptopy i komputery przenośne", SortOrder = 100 });
                await SaveChangesAsync();
            }

            // Zawsze upewnij się, że SortOrder kategorii odpowiada pożądanej kolejności
            await EnsureCategorySortOrdersAsync();
        }

        /// <summary>
        /// Aktualizuje SortOrder istniejących kategorii zgodnie z docelową kolejnością.
        /// Uruchamiane przy każdym starcie, naprawia dane ze starszych seedów.
        /// </summary>
        private async Task EnsureCategorySortOrdersAsync()
        {
            var desiredOrder = new System.Collections.Generic.Dictionary<string, int>
            {
                { "K",  1 }, { "M",  2 }, { "D",  3 }, { "S",  4 }, { "U",  5 },
                { "SR", 6 }, { "KS", 7 }, { "VI", 8 }, { "AP", 9 }, { "KK", 10 },
                { "TE", 11 }, { "SC", 12 }, { "KD", 13 }, { "TV", 14 }, { "W", 15 },
                { "TK", 16 }, { "L", 17 }, { "ND", 18 }, { "F",  99 }, { "LAP", 100 }
            };

            var categories = await Categories.ToListAsync();
            bool changed = false;
            foreach (var category in categories)
            {
                if (category.Code != null
                    && desiredOrder.TryGetValue(category.Code, out var order)
                    && category.SortOrder != order)
                {
                    category.SortOrder = order;
                    changed = true;
                }
            }

            if (changed)
                await SaveChangesAsync();
        }

        private async Task SeedSampleEquipmentAsync()
        {
            var computerCategory = await Categories.FirstOrDefaultAsync(c => c.Code == "K");
            var monitorCategory = await Categories.FirstOrDefaultAsync(c => c.Code == "M");
            var zamoscLocation = await Locations.FirstOrDefaultAsync(l => l.Code == "86");
            var krosnoLocation = await Locations.FirstOrDefaultAsync(l => l.Code == "99");
            var kierownikDepartment = await Departments.FirstOrDefaultAsync(d => d.Code == "KIE");
            var kasaDepartment = await Departments.FirstOrDefaultAsync(d => d.Code == "KAS");

            if (computerCategory != null && zamoscLocation != null)
            {
                var sampleEquipment = new List<Equipment>
                {
                    new Equipment 
                    { 
                        InventoryNumber = "K86001",
                        Name = "Komputer Dell OptiPlex 7090",
                        Description = "Komputer biurowy Dell OptiPlex 7090",
                        Brand = "Dell",
                        Model = "OptiPlex 7090",
                        SerialNumber = "DELL001",
                        Status = EquipmentStatus.Active,
                        CategoryId = computerCategory.Id,
                        LocationId = zamoscLocation.Id,
                        DepartmentId = kierownikDepartment?.Id,
                        PurchaseDate = DateTime.Now.AddMonths(-6),
                        PurchasePrice = 2500.00m,
                        IpAddress = "192.168.1.100"
                    },
                    new Equipment 
                    { 
                        InventoryNumber = "K86002",
                        Name = "Laptop Lenovo ThinkPad E15",
                        Description = "Laptop biznesowy Lenovo ThinkPad E15",
                        Brand = "Lenovo",
                        Model = "ThinkPad E15",
                        SerialNumber = "LEN001",
                        Status = EquipmentStatus.Active,
                        CategoryId = computerCategory.Id,
                        LocationId = zamoscLocation.Id,
                        DepartmentId = kasaDepartment?.Id,
                        PurchaseDate = DateTime.Now.AddMonths(-3),
                        PurchasePrice = 3200.00m
                    }
                };

                if (monitorCategory != null)
                {
                    var monitor = new Equipment 
                    { 
                        InventoryNumber = "M86001",
                        Name = "Monitor Samsung 24\"",
                        Description = "Monitor Samsung 24\" Full HD",
                        Brand = "Samsung",
                        Model = "F24T450FQU",
                        SerialNumber = "SAM001",
                        Status = EquipmentStatus.Active,
                        CategoryId = monitorCategory.Id,
                        LocationId = zamoscLocation.Id,
                        DepartmentId = kierownikDepartment?.Id,
                        PurchaseDate = DateTime.Now.AddMonths(-4),
                        PurchasePrice = 650.00m
                    };

                    sampleEquipment.Add(monitor);
                }

                Equipment.AddRange(sampleEquipment);
                await SaveChangesAsync();
            }
        }
    }
}