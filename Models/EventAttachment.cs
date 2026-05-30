using System;
using System.ComponentModel.DataAnnotations;

namespace InwentaryzacjaSprzetu.Models
{
    public class EventAttachment
    {
        public int Id { get; set; }

        public int EventId { get; set; }
        public InventoryEvent Event { get; set; } = null!;

        /// <summary>Oryginalna nazwa pliku — wyświetlana użytkownikowi.</summary>
        [Required]
        [StringLength(500)]
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>Nazwa pod jaką plik został zapisany w folderze załączników (GUID-based).</summary>
        [Required]
        [StringLength(500)]
        public string StoredFileName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
