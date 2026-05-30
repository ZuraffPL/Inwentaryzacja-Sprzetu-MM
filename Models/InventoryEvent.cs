using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace InwentaryzacjaSprzetu.Models
{
    public class InventoryEvent
    {
        public int Id { get; set; }

        public InventoryEventType EventType { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime EventDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? PerformedBy { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        /// <summary>Cena zakupu — wypełniana przy zdarzeniu Purchase, synchronizowana z Equipment.PurchasePrice</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PurchasePrice { get; set; }

        /// <summary>Status sprzętu przed zmianą — wypełniany przy zdarzeniu StatusChange (dane historyczne)</summary>
        public EquipmentStatus? PreviousStatus { get; set; }

        /// <summary>Nowy status sprzętu — wypełniany przy zdarzeniu StatusChange, synchronizowany z Equipment.Status</summary>
        public EquipmentStatus? NewStatus { get; set; }

        /// <summary>Nazwa działu przed zmianą — wypełniana przy zdarzeniu DepartmentChange (przechowywana jako tekst — historia jest bezpieczna mimo zmiany nazwy działu)</summary>
        public string? PreviousDepartmentName { get; set; }

        /// <summary>Nazwa nowego działu — wypełniana przy zdarzeniu DepartmentChange, synchronizowana z Equipment.DepartmentId</summary>
        public string? NewDepartmentName { get; set; }
        /// <summary>Poprzedni nr inwentarzowy — wypełniany przy zdarzeniu InventoryNumberChange (może być wpisany ręcznie)</summary>
        [StringLength(100)]
        public string? PreviousInventoryNumber { get; set; }

        /// <summary>Nowy nr inwentarzowy — wypełniany przy zdarzeniu InventoryNumberChange, synchronizowany z Equipment.InventoryNumber</summary>
        [StringLength(100)]
        public string? NewInventoryNumber { get; set; }

        /// <summary>Poprzedni adres IP — auto-wypełniany z Equipment.IpAddress przy tworzeniu zdarzenia IpAddressChange</summary>
        [StringLength(45)]
        public string? PreviousIpAddress { get; set; }

        /// <summary>Nowy adres IP — wpisywany ręcznie; synchronizowany z Equipment.IpAddress przy zapisie</summary>
        [StringLength(45)]
        public string? NewIpAddress { get; set; }

        /// <summary>Poprzedni nr unikatowy kasy fiskalnej — auto-wypełniany z Equipment.SerialNumber przy tworzeniu zdarzenia FiscalNumberChange</summary>
        [StringLength(100)]
        public string? PreviousSerialNumber { get; set; }

        /// <summary>Nowy nr unikatowy kasy fiskalnej — wpisywany ręcznie; synchronizowany z Equipment.SerialNumber przy zapisie</summary>
        [StringLength(100)]
        public string? NewSerialNumber { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>Status zdarzenia: Aktywne (domyślne) lub Zakończone (archiwum)</summary>
        public EventStatus EventStatus { get; set; } = EventStatus.Active;

        // Klucze obce
        public int? EquipmentId { get; set; }
        public int? LocationId { get; set; }

        // Nawigacja
        public virtual Equipment? Equipment { get; set; }
        public virtual Location? Location { get; set; }
        public virtual ICollection<EventAttachment> Attachments { get; set; } = new List<EventAttachment>();

        /// <summary>True jeśli zdarzenie ma co najmniej jeden załącznik (wymaga Include(Attachments) przy ładowaniu).</summary>
        [NotMapped]
        public bool HasAttachments => Attachments.Any();

        private static readonly Dictionary<EquipmentStatus, string> StatusLabels = new()
        {
            [EquipmentStatus.Active] = "Sprawny",
            [EquipmentStatus.Inactive] = "Rezerwa",
            [EquipmentStatus.UnderMaintenance] = "W konserwacji",
            [EquipmentStatus.Damaged] = "Zepsuty (uszkodzony)",
            [EquipmentStatus.Disposed] = "Zutylizowany (kasacja)",
        };

        /// <summary>
        /// Wyświetlany ciąg znaków opisujący zmianę:
        /// - "Rezerwa → Sprawny" dla zdarzeń StatusChange
        /// - "Magazyn → Sprzedaż" dla zdarzeń DepartmentChange
        /// </summary>
        [NotMapped]
        public string ChangeSummary
        {
            get
            {
                if (EventType == InventoryEventType.StatusChange)
                {
                    var prev = PreviousStatus.HasValue && StatusLabels.TryGetValue(PreviousStatus.Value, out var p) ? p : "—";
                    var next = NewStatus.HasValue && StatusLabels.TryGetValue(NewStatus.Value, out var n) ? n : "—";
                    return $"{prev} → {next}";
                }
                if (EventType == InventoryEventType.DepartmentChange)
                {
                    var prev = string.IsNullOrEmpty(PreviousDepartmentName) ? "—" : PreviousDepartmentName;
                    var next = string.IsNullOrEmpty(NewDepartmentName) ? "—" : NewDepartmentName;
                    return $"{prev} → {next}";
                }
                if (EventType == InventoryEventType.Audit)
                {
                    var prev = string.IsNullOrEmpty(PreviousInventoryNumber) ? "—" : PreviousInventoryNumber;
                    var next = string.IsNullOrEmpty(NewInventoryNumber) ? "—" : NewInventoryNumber;
                    return $"{prev} → {next}";
                }
                if (EventType == InventoryEventType.IpAddressChange)
                {
                    var prev = string.IsNullOrEmpty(PreviousIpAddress) ? "—" : PreviousIpAddress;
                    var next = string.IsNullOrEmpty(NewIpAddress) ? "—" : NewIpAddress;
                    return $"{prev} → {next}";
                }
                if (EventType == InventoryEventType.InventoryNumberAssignment)
                {
                    var next = string.IsNullOrEmpty(NewInventoryNumber) ? "—" : NewInventoryNumber;
                    return $"— → {next}";
                }
                if (EventType == InventoryEventType.FiscalNumberChange)
                {
                    var prev = string.IsNullOrEmpty(PreviousSerialNumber) ? "—" : PreviousSerialNumber;
                    var next = string.IsNullOrEmpty(NewSerialNumber) ? "—" : NewSerialNumber;
                    return $"{prev} → {next}";
                }
                return string.Empty;
            }
        }
    }

    public enum InventoryEventType
    {
        Purchase = 1,
        Transfer = 2,
        Maintenance = 3,
        Repair = 4,
        Disposal = 5,
        Audit = 6,
        StatusChange = 7,
        DepartmentChange = 8,
        IpAddressChange = 9,
        FiscalNumberChange = 10,
        InventoryNumberAssignment = 11,
        Other = 99
    }

    public enum EventStatus
    {
        Active = 1,
        Completed = 2
    }
}