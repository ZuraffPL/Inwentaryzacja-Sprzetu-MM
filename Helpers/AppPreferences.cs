using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace InwentaryzacjaSprzetu.Helpers
{
    /// <summary>
    /// Proste preferencje UI persystowane jako JSON w AppData użytkownika.
    /// </summary>
    public class AppPreferences
    {
        private static readonly string _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "InwentaryzacjaSprzetu",
            "preferences.json");

        /// <summary>Czy grupy sprzętu (kategorie) są rozwinięte w widoku głównym.</summary>
        public bool EquipmentGroupsExpanded { get; set; } = true;

        /// <summary>
        /// Kody krajów (PL, SK, CZ…) których zakładki są UKRYTE w pasku krajów widoku sprzętu.
        /// Pusta lista = wszystkie kraje widoczne.
        /// </summary>
        public List<string> HiddenCountryCodes { get; set; } = new();

        /// <summary>
        /// Wersja aplikacji, dla której użytkownik wyłączył automatyczne pokazywanie changeloga przy starcie.
        /// Jeśli zgodna z bieżącą wersją — okno nie pojawi się przy uruchomieniu.
        /// </summary>
        public string? ChangelogDismissedVersion { get; set; }

        /// <summary>Kody działów zaznaczonych w filtrze widoku sprzętu (puste = brak filtra).</summary>
        public List<string> EquipmentFilterDepartmentCodes { get; set; } = new();

        /// <summary>Wartości enum EquipmentStatus zaznaczonych w filtrze statusu (puste = brak filtra).</summary>
        public List<int> EquipmentFilterStatusValues { get; set; } = new();

        /// <summary>Kody kategorii zaznaczonych w filtrze kategorii sprzętu (puste = brak filtra).</summary>
        public List<string> EquipmentFilterCategoryCodes { get; set; } = new();

        /// <summary>Folder przechowywania załączników zdarzeń. Null lub pusty = niezdefiniowany (użytkownik zostanie zapytany przy pierwszym użyciu).</summary>
        public string? AttachmentsFolder { get; set; }

        /// <summary>
        /// Data (format yyyy-MM-dd), w której użytkownik kliknął „Nie pokazuj dziś ponownie"
        /// w oknie startowych powiadomień. Jeśli zgodna z bieżącą datą — okno nie pojawi się przy uruchomieniu.
        /// </summary>
        public string? AlertStartupDismissedDate { get; set; }

        /// <summary>
        /// Czy aplikacja ma uruchamiać się razem z systemem Windows (wpis w rejestrze HKCU Run).
        /// Odzwierciedla tylko stan preferencji — faktyczny wpis w rejestrze zarządza AutostartHelper.
        /// </summary>
        public bool RunOnStartup { get; set; } = false;

        public static AppPreferences Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    return JsonSerializer.Deserialize<AppPreferences>(json) ?? new AppPreferences();
                }
            }
            catch { /* plik uszkodzony lub brak dostępu — używamy domyślnych */ }
            return new AppPreferences();
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(_filePath)!;
                Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch { /* błąd zapisu preferencji — niekrytyczny */ }
        }
    }
}
