using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace InwentaryzacjaSprzetu.Models
{
    public class Department
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Nawigacja
        public virtual ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
    }
}