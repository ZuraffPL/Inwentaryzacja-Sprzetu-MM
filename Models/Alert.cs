using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InwentaryzacjaSprzetu.Models
{
    /// <summary>
    /// Powiadomienie / alert dla użytkownika.
    /// Może być powiązane z konkretnym sprzętem, całą kategorią lub ogólne.
    /// Staje się "aktywne" (widoczne w banerze) gdy TriggerDate &lt;= DateTime.Today i IsArchived == false.
    /// </summary>
    public class Alert
    {
        public int Id { get; set; }

        /// <summary>Krótka nazwa powiadomienia, np. "Legalizacja wagi".</summary>
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Opcjonalny opis wewnętrzny widoczny tylko w zarządzaniu alertami.</summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>Treść wyświetlana użytkownikowi gdy powiadomienie jest aktywne.</summary>
        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>Data od której powiadomienie staje się widoczne (włącznie).</summary>
        public DateTime TriggerDate { get; set; } = DateTime.Today;

        /// <summary>Powiązany sprzęt — null gdy dotyczy kategorii lub jest ogólne.</summary>
        public int? EquipmentId { get; set; }

        /// <summary>Powiązana kategoria — null gdy dotyczy sprzętu lub jest ogólne.</summary>
        public int? CategoryId { get; set; }

        public bool IsArchived { get; set; }

        public DateTime? ArchivedDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Nawigacja
        public virtual Equipment? Equipment { get; set; }
        public virtual Category? Category { get; set; }

        /// <summary>True gdy data wyzwolenia jest dziś lub w przeszłości (nie z bazy).</summary>
        [NotMapped]
        public bool IsTriggered => TriggerDate.Date <= DateTime.Today;

        /// <summary>Czytelny opis celu powiadomienia dla UI.</summary>
        [NotMapped]
        public string TargetDescription
        {
            get
            {
                if (Equipment != null)
                    return $"Sprzęt: {Equipment.Name} ({Equipment.InventoryNumber})";
                if (Category != null)
                    return $"Kategoria: {Category.Name}";
                return "Ogólne";
            }
        }

        /// <summary>Etykieta statusu dla UI.</summary>
        [NotMapped]
        public string StatusLabel => IsArchived
            ? "Archiwum"
            : IsTriggered
                ? "⚠ Aktywne"
                : $"Zaplanowane ({TriggerDate:dd.MM.yyyy})";
    }
}
