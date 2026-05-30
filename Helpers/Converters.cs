using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using InwentaryzacjaSprzetu.Models;

namespace InwentaryzacjaSprzetu.Helpers
{
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && parameter is string parameterValue)
            {
                return stringValue.Equals(parameterValue, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && parameter is string parameterValue)
            {
                return parameterValue;
            }
            return Binding.DoNothing;
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string parameterValue)
            {
                if (parameterValue.Equals("Empty", StringComparison.OrdinalIgnoreCase))
                {
                    // Pokaż element gdy string jest pusty
                    return string.IsNullOrWhiteSpace(value?.ToString()) 
                        ? Visibility.Visible 
                        : Visibility.Collapsed;
                }
                else if (value is string stringValue)
                {
                    // Standardowe porównanie z parametrem
                    return stringValue.Equals(parameterValue, StringComparison.OrdinalIgnoreCase) 
                        ? Visibility.Visible 
                        : Visibility.Collapsed;
                }
            }
            
            // Domyślnie pokaż gdy wartość nie jest pusta
            return string.IsNullOrWhiteSpace(value?.ToString()) 
                ? Visibility.Collapsed 
                : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumToStringConverter : IValueConverter
    {
        private static readonly Dictionary<InventoryEventType, string> EventTypeNames = new()
        {
            { InventoryEventType.Purchase, "Zakup" },
            { InventoryEventType.Transfer, "Przeniesienie" },
            { InventoryEventType.Maintenance, "Konserwacja" },
            { InventoryEventType.Repair, "Naprawa" },
            { InventoryEventType.Disposal, "Utylizacja" },
            { InventoryEventType.Audit, "Zmiana nr inwentarzowego" },
            { InventoryEventType.StatusChange, "Zmiana statusu" },
            { InventoryEventType.DepartmentChange, "Zmiana działu" },
            { InventoryEventType.IpAddressChange, "Zmiana adresu IP" },
            { InventoryEventType.FiscalNumberChange, "Zmiana nr unikatowego kasy fiskalnej" },
            { InventoryEventType.InventoryNumberAssignment, "Nadanie nr inwentarzowego" },
            { InventoryEventType.Other, "Inne" }
        };

        private static readonly Dictionary<EquipmentStatus, string> StatusNames = new()
        {
            { EquipmentStatus.Active, "Sprawny" },
            { EquipmentStatus.Inactive, "Rezerwa" },
            { EquipmentStatus.UnderMaintenance, "W konserwacji" },
            { EquipmentStatus.Damaged, "Zepsuty (uszkodzony)" },
            { EquipmentStatus.Disposed, "Zutylizowany (kasacja)" }
        };

        private static readonly Dictionary<EventStatus, string> EventStatusNames = new()
        {
            { EventStatus.Active, "Aktywne" },
            { EventStatus.Completed, "Zakończone" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InventoryEventType eventType)
            {
                return EventTypeNames.TryGetValue(eventType, out var eventName) ? eventName : eventType.ToString();
            }
            if (value is EquipmentStatus status)
            {
                return StatusNames.TryGetValue(status, out var statusName) ? statusName : status.ToString();
            }
            if (value is EventStatus eventStatus)
            {
                return EventStatusNames.TryGetValue(eventStatus, out var esName) ? esName : eventStatus.ToString();
            }
            if (value is Enum enumValue)
            {
                return enumValue.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && targetType.IsEnum)
            {
                try
                {
                    return Enum.Parse(targetType, stringValue);
                }
                catch
                {
                    return Binding.DoNothing;
                }
            }
            return Binding.DoNothing;
        }
    }

    public class CategoryToPastelBackgroundConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> CategoryColors = new()
        {
            { "Komputery", "#E6F3FF" },      // Jasny niebieski
            { "Monitory", "#E6FFE6" },       // Jasny zielony
            { "Drukarki", "#FFE6F3" },       // Jasny różowy
            { "Serwery", "#FFF3E6" },        // Jasny pomarańczowy
            { "UPSy", "#F3E6FF" },           // Jasny fioletowy
            { "Skanery", "#E6FFFF" },        // Jasny cyan
            { "Ksero", "#FFFFE6" },          // Jasny żółty
            { "Voice IP", "#F0E6FF" },       // Jasny lawenda
            { "Router/Switch", "#E6FFF0" },  // Jasny miętowy
            { "Czytniki kodów kreskowych", "#FFE6E6" }, // Jasny łososiowy
            { "Testery Banknotów", "#F3FFE6" },         // Jasny limonkowy
            { "Sprawdzarki cen", "#E6E6FF" },           // Jasny periwinkle
            { "Kolektory Danych", "#FFEDE6" },          // Jasny brzoskwiniowy
            { "Telewizory", "#E6F0FF" },                // Jasny alice blue
            { "Wagi", "#F5FFE6" },                      // Jasny honeydew
            { "Telefony komórkowe", "#FFE6F0" },        // Jasny lavender blush
            { "Laminatory", "#E6FFF5" },                // Jasny mint cream
            { "Niszczarki", "#F0FFE6" }                 // Jasny light green
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string categoryName && CategoryColors.TryGetValue(categoryName, out var color))
            {
                return color;
            }
            return "#F8F8F8"; // Domyślny jasny szary
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IpAddressDisplayConverter : IValueConverter
    {
        private static readonly HashSet<string> NoIpCategoryCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "SC", "CK", "KD", "N", "SP", "TB", "M"
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Equipment equipment)
            {
                if (NoIpCategoryCodes.Contains(equipment.Category?.Code ?? string.Empty))
                    return "N/A";
                return equipment.IpAddress ?? string.Empty;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ConnectedComputerDisplayConverter : IValueConverter
    {
        private static readonly HashSet<string> ConnectedCategoryCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "M", "SC", "CK", "D", "U", "SR", "F", "TE", "KK", "KD"
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Equipment equipment)
            {
                if (!ConnectedCategoryCodes.Contains(equipment.Category?.Code ?? string.Empty))
                    return "N/A";
                return equipment.ConnectedToEquipment?.InventoryNumber ?? string.Empty;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class EventTypeToPastelBackgroundConverter : IValueConverter
    {
        private static readonly Dictionary<string, string> EventTypeColors = new()
        {
            { "Zakup",          "#E6F3FF" }, // Jasny niebieski
            { "Przeniesienie",  "#E6FFE6" }, // Jasny zielony
            { "Konserwacja",    "#FFF3E6" }, // Jasny pomarańczowy
            { "Naprawa",        "#FFE6E6" }, // Jasny czerwony
            { "Utylizacja",     "#F3E6FF" }, // Jasny fioletowy
            { "Zmiana nr inwentarzowego", "#E6FFFF" }, // Jasny cyan
            { "Zmiana statusu", "#FFFFE6" }, // Jasny żółty
            { "Zmiana działu",  "#FFE8D6" }, // Jasny brzoskwiniowy
            { "Zmiana adresu IP", "#E6F0E6" }, // Jasny szaro-zielony
            { "Zmiana nr unikatowego kasy fiskalnej", "#FFF0E6" }, // Jasny pomarańczowy
            { "Nadanie nr inwentarzowego", "#F0E6FF" }, // Jasny fioletowy
            { "Inne",           "#F0F0F0" }, // Jasny szary
        };

        private static readonly Dictionary<InventoryEventType, string> EnumToColor = new()
        {
            { InventoryEventType.Purchase,     "#E6F3FF" },
            { InventoryEventType.Transfer,     "#E6FFE6" },
            { InventoryEventType.Maintenance,  "#FFF3E6" },
            { InventoryEventType.Repair,       "#FFE6E6" },
            { InventoryEventType.Disposal,     "#F3E6FF" },
            { InventoryEventType.Audit,        "#E6FFFF" },
            { InventoryEventType.StatusChange,      "#FFFFE6" },
            { InventoryEventType.DepartmentChange,  "#FFE8D6" },
            { InventoryEventType.IpAddressChange,   "#E6F0E6" },
            { InventoryEventType.FiscalNumberChange, "#FFF0E6" },
            { InventoryEventType.InventoryNumberAssignment, "#F0E6FF" },
            { InventoryEventType.Other,             "#F0F0F0" },
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Grupowanie przekazuje wartość enum bezpośrednio jako Name grupy
            if (value is InventoryEventType eventType && EnumToColor.TryGetValue(eventType, out var colorByEnum))
                return colorByEnum;
            if (value is string typeName && EventTypeColors.TryGetValue(typeName, out var colorByName))
                return colorByName;
            return "#F8F8F8";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Collapsed;
            }
            return false;
        }
    }

    /// <summary>Zamienia EventStatus na kolor tekstu: Aktywne = zielony, Zakończone = szary</summary>
    public class EventStatusToForegroundConverter : IValueConverter
    {
        private static readonly SolidColorBrush ActiveBrush    = new(Color.FromRgb(0x2E, 0x7D, 0x32)); // #2E7D32 zielony
        private static readonly SolidColorBrush CompletedBrush = new(Color.FromRgb(0x9E, 0x9E, 0x9E)); // #9E9E9E szary

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EventStatus status)
                return status == EventStatus.Active ? ActiveBrush : CompletedBrush;
            return CompletedBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Dla danej nazwy kategorii zwraca etykiety kolumn Marka/Model/Nr Seryjny
    /// specyficzne dla tej kategorii (np. dla Komputerów: System | RAM | Procesor).
    /// Zwraca pusty string dla kategorii bez specjalnych etykiet — TextBlock ukrywa się przez trigger.
    /// </summary>
    public class CategoryNameToColumnLabelsConverter : IValueConverter
    {
        // Mapowanie nazwy kategorii → etykiety kolumn (Brand, Model, SerialNumber)
        private static readonly Dictionary<string, string> CategoryColumnLabels = new()
        {
            { "Komputery",          "System   |   RAM   |   Procesor"      },
            { "Monitory",           "Marka   |   Model   |   Przekątna"    },
            { "Kasy Fiskalne",      "Marka   |   Model   |   Nr unikatowy" },
            { "Telefony komórkowe", "Marka   |   Model   |   IMEI"         }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string categoryName)
                return CategoryColumnLabels.TryGetValue(categoryName, out var labels) ? labels : string.Empty;
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Konwertuje wartość null/not-null na Visibility.
    /// Bez parametru: Visible gdy nie-null, Collapsed gdy null.
    /// Z parametrem "Invert": Visible gdy null, Collapsed gdy nie-null.
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null;
            bool invert = parameter is string p && p.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            bool visible = invert ? isNull : !isNull;
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Konwertuje kolekcję Items z grupy CollectionViewGroup na tekst podsumowania nagłówka,
    /// np. " (5 pozycji, Aktywne zdarzenia: 3)". Gdy brak aktywnych zdarzeń — zwraca " (5 pozycji)".
    /// </summary>
    public class GroupItemsToSummaryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not System.Collections.IEnumerable items)
                return string.Empty;

            int total = 0;
            int activeEvents = 0;
            foreach (var item in items)
            {
                total++;
                if (item is InwentaryzacjaSprzetu.Models.Equipment eq && eq.HasActiveEvents)
                    activeEvents++;
            }

            if (activeEvents > 0)
                return $" ({total} pozycji, Aktywne zdarzenia: {activeEvents})";

            return $" ({total} pozycji)";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Proxy do przekazywania DataContext do elementów poza drzewem wizualnym</summary>
    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore() => new BindingProxy();

        public object? Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
    }

    /// <summary>Zwraca emoji-ikonę na podstawie rozszerzenia nazwy pliku (np. .pdf → 📄, .jpg → 🖼️).</summary>
    public class FileExtToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string fileName) return "📎";
            var ext = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".pdf"  => "📄",
                ".jpg"  => "🖼️",
                ".jpeg" => "🖼️",
                ".png"  => "🖼️",
                ".webp" => "🖼️",
                _       => "📎"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}