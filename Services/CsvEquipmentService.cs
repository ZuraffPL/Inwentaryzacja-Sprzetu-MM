using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using InwentaryzacjaSprzetu.Models;

namespace InwentaryzacjaSprzetu.Services
{
    /// <summary>
    /// Eksport i import sprzętu do/z pliku CSV.
    /// Separator: średnik (;). Encoding: UTF-8 z BOM (czytelny w Excel).
    /// Format daty: yyyy-MM-dd. Format ceny: InvariantCulture (kropka jako separator dziesiętny).
    /// </summary>
    public static class CsvEquipmentService
    {
        private const char Sep = ';';

        private static readonly string[] Headers =
        {
            "NrInwentarzowy", "BrakNrInwentarzowego", "Nazwa", "Opis", "Marka", "Model",
            "NrSeryjny", "AdresIP", "Kategoria", "Lokalizacja", "Dzial", "Status",
            "CenaNabycia", "DataNabycia", "DataKoncaGwarancji", "Uwagi", "SzczegolyLokalizacji"
        };

        // ─── EKSPORT ──────────────────────────────────────────────────────────

        public static string Export(IEnumerable<Equipment> equipment)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(Sep, Headers));

            foreach (var e in equipment)
            {
                var fields = new[]
                {
                    e.InventoryNumber,
                    e.NoInventoryNumber ? "TAK" : "",
                    e.Name,
                    e.Description ?? "",
                    e.Brand ?? "",
                    e.Model ?? "",
                    e.SerialNumber ?? "",
                    e.IpAddress ?? "",
                    e.Category?.Name ?? "",
                    e.Location?.Name ?? "",
                    e.Department?.Name ?? "",
                    StatusToPolish(e.Status),
                    e.PurchasePrice?.ToString("F2", CultureInfo.InvariantCulture) ?? "",
                    e.PurchaseDate?.ToString("yyyy-MM-dd") ?? "",
                    e.WarrantyEndDate?.ToString("yyyy-MM-dd") ?? "",
                    e.Notes ?? "",
                    e.LocationDetails ?? ""
                };

                sb.AppendLine(string.Join(Sep, fields.Select(QuoteField)));
            }

            return sb.ToString();
        }

        // ─── IMPORT ───────────────────────────────────────────────────────────

        public record ImportResult(List<Equipment> Success, List<string> Errors);

        public static ImportResult Import(
            string csvContent,
            IEnumerable<Category> categories,
            IEnumerable<Location> locations,
            IEnumerable<Department> departments)
        {
            var success = new List<Equipment>();
            var errors  = new List<string>();

            // Słowniki do wyszukiwania po nazwie (bez rozróżniania wielkości liter)
            var catMap  = categories.ToDictionary(c => c.Name.Trim().ToLowerInvariant(), c => c);
            var locMap  = locations.ToDictionary(l => l.Name.Trim().ToLowerInvariant(), l => l);
            var deptMap = departments.ToDictionary(d => d.Name.Trim().ToLowerInvariant(), d => d);

            using var reader = new StringReader(csvContent);

            // Pierwsza linia = nagłówek
            string? headerLine = reader.ReadLine();
            if (headerLine == null)
                return new ImportResult(success, errors);

            var headerCols = ParseLine(headerLine);
            var colIndex   = BuildColumnIndex(headerCols);

            int lineNum = 1;
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                lineNum++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = ParseLine(line);
                try
                {
                    var item = ParseEquipment(cols, colIndex, catMap, locMap, deptMap);
                    success.Add(item);
                }
                catch (Exception ex)
                {
                    errors.Add($"Wiersz {lineNum}: {ex.Message}");
                }
            }

            return new ImportResult(success, errors);
        }

        // ─── POMOCNICZE ───────────────────────────────────────────────────────

        private static Equipment ParseEquipment(
            string[] cols,
            Dictionary<string, int> colIndex,
            Dictionary<string, Category> catMap,
            Dictionary<string, Location> locMap,
            Dictionary<string, Department> deptMap)
        {
            string Get(string name) =>
                colIndex.TryGetValue(name.ToLowerInvariant(), out var i) && i < cols.Length
                    ? cols[i].Trim()
                    : "";

            // ─ Kategoria (wymagana)
            var categoryName = Get("kategoria");
            if (!catMap.TryGetValue(categoryName.ToLowerInvariant(), out var category))
                throw new InvalidOperationException($"Nieznana kategoria: '{categoryName}'");

            // ─ Lokalizacja (wymagana)
            var locationName = Get("lokalizacja");
            if (!locMap.TryGetValue(locationName.ToLowerInvariant(), out var location))
                throw new InvalidOperationException($"Nieznana lokalizacja: '{locationName}'");

            // ─ Dział (opcjonalny)
            Department? department = null;
            var deptName = Get("dzial");
            if (!string.IsNullOrWhiteSpace(deptName))
                deptMap.TryGetValue(deptName.ToLowerInvariant(), out department);

            // ─ Numer inwentarzowy
            bool brakNr = Get("braknrinwentarzowego").Equals("TAK", StringComparison.OrdinalIgnoreCase);
            var invNr   = Get("nrinwentarzowy");
            if (!brakNr && string.IsNullOrWhiteSpace(invNr))
                throw new InvalidOperationException(
                    "Pole NrInwentarzowy jest puste, a BrakNrInwentarzowego nie jest ustawione na TAK.");

            // ─ Nazwa (wymagana)
            var name = Get("nazwa");
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Brak nazwy sprzętu (kolumna Nazwa jest pusta).");

            // ─ Cena
            decimal? price = null;
            var priceStr = Get("cenanabycia");
            if (!string.IsNullOrWhiteSpace(priceStr) &&
                decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var p))
                price = p;

            // ─ Daty
            DateTime? purchaseDate = null;
            var dateStr = Get("datanabycia");
            if (!string.IsNullOrWhiteSpace(dateStr) &&
                DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                purchaseDate = d;

            DateTime? warrantyDate = null;
            var warrantyStr = Get("datakoncagwarancji");
            if (!string.IsNullOrWhiteSpace(warrantyStr) &&
                DateTime.TryParseExact(warrantyStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var w))
                warrantyDate = w;

            // ─ Status
            var status = PolishToStatus(Get("status"));

            return new Equipment
            {
                InventoryNumber = invNr,
                NoInventoryNumber = brakNr,
                Name             = name,
                Description      = NullIfEmpty(Get("opis")),
                Brand            = NullIfEmpty(Get("marka")),
                Model            = NullIfEmpty(Get("model")),
                SerialNumber     = NullIfEmpty(Get("nrseryjny")),
                IpAddress        = NullIfEmpty(Get("adresip")),
                CategoryId       = category.Id,
                Category         = category,
                LocationId       = location.Id,
                Location         = location,
                DepartmentId     = department?.Id,
                Department       = department,
                Status           = status,
                PurchasePrice    = price,
                PurchaseDate     = purchaseDate,
                WarrantyEndDate  = warrantyDate,
                Notes            = NullIfEmpty(Get("uwagi")),
                LocationDetails  = NullIfEmpty(Get("szczegolyLokalizacji")),
                CreatedDate      = DateTime.Now
            };
        }

        private static Dictionary<string, int> BuildColumnIndex(string[] headers)
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                var key = headers[i].Trim();
                if (!dict.ContainsKey(key))
                    dict[key] = i;
            }
            return dict;
        }

        /// <summary>
        /// Parsuje pojedynczą linię CSV z obsługą pól w cudzysłowach (RFC 4180).
        /// Podwójny cudzysłów ("") wewnątrz pola traktowany jako jeden cudzysłów.
        /// </summary>
        private static string[] ParseLine(string line)
        {
            var result  = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            int i = 0;

            while (i < line.Length)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i += 2;
                        }
                        else
                        {
                            inQuotes = false;
                            i++;
                        }
                    }
                    else
                    {
                        current.Append(c);
                        i++;
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                        i++;
                    }
                    else if (c == Sep)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                        i++;
                    }
                    else
                    {
                        current.Append(c);
                        i++;
                    }
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }

        private static string QuoteField(string field)
        {
            if (field.Contains(Sep) || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            return field;
        }

        private static string? NullIfEmpty(string s) =>
            string.IsNullOrWhiteSpace(s) ? null : s;

        private static string StatusToPolish(EquipmentStatus status) => status switch
        {
            EquipmentStatus.Active           => "Sprawny",
            EquipmentStatus.Inactive         => "Rezerwa",
            EquipmentStatus.UnderMaintenance => "W konserwacji",
            EquipmentStatus.Damaged          => "Zepsuty",
            EquipmentStatus.Disposed         => "Zutylizowany",
            _                                => status.ToString()
        };

        private static EquipmentStatus PolishToStatus(string s) =>
            s.Trim().ToLowerInvariant() switch
            {
                "sprawny"         => EquipmentStatus.Active,
                "rezerwa"         => EquipmentStatus.Inactive,
                "w konserwacji"   => EquipmentStatus.UnderMaintenance,
                "zepsuty"         => EquipmentStatus.Damaged,
                "uszkodzony"      => EquipmentStatus.Damaged,
                "zutylizowany"    => EquipmentStatus.Disposed,
                "kasacja"         => EquipmentStatus.Disposed,
                _                 => EquipmentStatus.Active
            };
    }
}
