using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InwentaryzacjaSprzetu.Models
{
    public class Location
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Code { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>Krótki alfanumeryczny kod pawilonu (max 5 znaków), np. "MZ87"</summary>
        [StringLength(5)]
        public string? PavilionCode { get; set; }

        /// <summary>Kod kraju (PL, SK, CZ, RO, HU, HR) — decyduje o grupowaniu na zakładkach krajów</summary>
        [StringLength(5)]
        public string? CountryCode { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Nawigacja
        public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
        public virtual ICollection<InventoryEvent> InventoryEvents { get; set; } = new List<InventoryEvent>();
    }
}