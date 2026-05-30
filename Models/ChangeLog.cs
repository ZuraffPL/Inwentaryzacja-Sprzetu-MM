using System;
using System.ComponentModel.DataAnnotations;

namespace InwentaryzacjaSprzetu.Models
{
    public class ChangeLog
    {
        public int Id { get; set; }

        public int EquipmentId { get; set; }

        [Required]
        [StringLength(100)]
        public string FieldName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? OldValue { get; set; }

        [StringLength(1000)]
        public string? NewValue { get; set; }

        public DateTime ChangeDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? ChangedBy { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        // Nawigacja
        public virtual Equipment Equipment { get; set; } = null!;
    }
}