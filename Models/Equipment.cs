using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace InwentaryzacjaSprzetu.Models
{
    public class Equipment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string InventoryNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Brand { get; set; }

        [StringLength(100)]
        public string? Model { get; set; }

        [StringLength(100)]
        public string? SerialNumber { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        public int? DepartmentId { get; set; }

        // Powiązanie sprzętu (np. monitor podłączony do komputera)
        public int? ConnectedToEquipmentId { get; set; }

        [StringLength(200)]
        public string? LocationDetails { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PurchasePrice { get; set; }

        public DateTime? PurchaseDate { get; set; }

        public DateTime? WarrantyEndDate { get; set; }

        public EquipmentStatus Status { get; set; } = EquipmentStatus.Active;

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastModifiedDate { get; set; }

        /// <summary>True gdy sprzęt nie posiada numeru inwentarzowego (nieczytelny lub nienadany)</summary>
        public bool NoInventoryNumber { get; set; } = false;

        // Klucze obce
        public int CategoryId { get; set; }
        public int LocationId { get; set; }

        // Nawigacja
        public virtual Category Category { get; set; } = null!;
        public virtual Location Location { get; set; } = null!;
        public virtual Department? Department { get; set; }
        public virtual Equipment? ConnectedToEquipment { get; set; }
        public virtual ICollection<InventoryEvent> InventoryEvents { get; set; } = new List<InventoryEvent>();
        public virtual ICollection<ChangeLog> ChangeLogs { get; set; } = new List<ChangeLog>();

        // Klucz sortowania numerycznego po numerze inwentarzowym (np. "10/S/87/K" → 10)
        [NotMapped]
        public int InventoryNumberSortKey
        {
            get
            {
                var slash = InventoryNumber?.IndexOf('/');
                if (slash > 0 && int.TryParse(InventoryNumber![..slash.Value], out var num))
                    return num;
                return 0;
            }
        }

        /// <summary>True gdy sprzęt ma co najmniej jedno aktywne zdarzenie</summary>
        [NotMapped]
        public bool HasActiveEvents =>
            InventoryEvents != null && InventoryEvents.Any(e => e.EventStatus == EventStatus.Active);

        /// <summary>True gdy sprzęt ma zdarzenia ale żadne nie jest aktywne (wszystkie archiwalne)</summary>
        [NotMapped]
        public bool HasArchivedEventsOnly =>
            InventoryEvents != null && InventoryEvents.Any() && !InventoryEvents.Any(e => e.EventStatus == EventStatus.Active);
    }

    public enum EquipmentStatus
    {
        Active = 1,
        Inactive = 2,
        UnderMaintenance = 3,
        Damaged = 4,
        Disposed = 5
    }
}