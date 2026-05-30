using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InwentaryzacjaSprzetu.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string? Code { get; set; }

        public int SortOrder { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        // Nawigacja
        public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
    }
}