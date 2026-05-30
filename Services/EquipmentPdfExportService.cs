using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using InwentaryzacjaSprzetu.Models;

namespace InwentaryzacjaSprzetu.Services
{
    /// <summary>
    /// Generuje plik PDF z listą sprzętu w układzie poziomym (A4 Landscape).
    /// Obsługuje aktualne filtry: pawilonu, działu, statusu.
    /// </summary>
    public static class EquipmentPdfExportService
    {
        // Mapowanie statusów na czytelne nazwy (te same co EnumToStringConverter)
        private static readonly Dictionary<EquipmentStatus, string> StatusLabels = new()
        {
            { EquipmentStatus.Active,           "Sprawny"                },
            { EquipmentStatus.Inactive,         "Rezerwa"                },
            { EquipmentStatus.UnderMaintenance, "W konserwacji"          },
            { EquipmentStatus.Damaged,          "Zepsuty (uszkodzony)"  },
            { EquipmentStatus.Disposed,         "Zutylizowany (kasacja)" },
        };

        /// <param name="outputPath">Ścieżka pliku .pdf do zapisu.</param>
        /// <param name="items">Sprzęt do wyeksportowania (już przefiltrowany).</param>
        /// <param name="reportTitle">Tytuł nagłówka — np. "Inwentaryzacja sprzętu – PL-001".</param>
        /// <param name="filterDescription">Opis aktywnych filtrów; null gdy brak filtrów.</param>
        /// <param name="hideDepartmentColumn">Ukryj kolumnę Dział (filtr działu aktywny).</param>
        /// <param name="hideStatusColumn">Ukryj kolumnę Status (filtr statusu aktywny).</param>
        public static void Export(
            string outputPath,
            IEnumerable<Equipment> items,
            string reportTitle,
            string? filterDescription = null,
            bool hideDepartmentColumn = false,
            bool hideStatusColumn = false)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var rows = items.OrderBy(e => e.Category?.SortOrder ?? 0)
                            .ThenBy(e => e.InventoryNumberSortKey)
                            .ToList();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(5, Unit.Point);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                    page.Header().Element(ComposeHeader(reportTitle, filterDescription, rows.Count));
                    page.Content().Element(ComposeTable(rows, hideDepartmentColumn, hideStatusColumn));
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Strona ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                        text.Span($"   •   Wygenerowano: {DateTime.Now:dd.MM.yyyy HH:mm}");
                    });
                });
            }).GeneratePdf(outputPath);
        }

        // ── Nagłówek dokumentu ──────────────────────────────────────────────────

        private static Action<IContainer> ComposeHeader(string title, string? filterDesc, int totalCount)
        {
            return container =>
            {
                container.Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text(title)
                           .FontSize(14).Bold().FontColor(Colors.Grey.Darken3);
                        row.ConstantItem(200).AlignRight()
                           .Text($"Łącznie pozycji: {totalCount}")
                           .FontSize(9).FontColor(Colors.Grey.Medium);
                    });

                    if (!string.IsNullOrWhiteSpace(filterDesc))
                    {
                        col.Item().PaddingTop(2)
                           .Text($"Filtry: {filterDesc}")
                           .FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                    }

                    col.Item().PaddingTop(4)
                       .LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                });
            };
        }

        // ── Tabela ──────────────────────────────────────────────────────────────

        private static Action<IContainer> ComposeTable(List<Equipment> rows, bool hideDept, bool hideStatus)
        {
            // Pełna lista kolumn — filtrujemy wg aktywnych filtrów
            var allColumns = new (string Header, float Weight, Func<Equipment, string> Value, bool Include)[]
            {
                ("Nr inw.",    6.0f, e => e.InventoryNumber,                                                                                                             true),
                ("Nazwa",     15.0f, e => e.Name,                                                                                                                        true),
                ("Marka",      6.5f, e => e.Brand ?? "",                                                                                                                 true),
                ("Model",      7.0f, e => e.Model ?? "",                                                                                                                 true),
                ("Nr seryjny", 7.5f, e => e.SerialNumber ?? "",                                                                                                          true),
                ("Adres IP",   6.5f, e => FormatIp(e),                                                                                                                  true),
                ("Dział",      6.0f, e => e.Department?.Name ?? "",                                                                                                      !hideDept),
                ("Gdzie?",     7.0f, e => e.LocationDetails ?? "",                                                                                                       true),
                ("Status",     7.0f, e => StatusLabels.TryGetValue(e.Status, out var s) ? s : e.Status.ToString(),                                                       !hideStatus),
                ("Cena",       5.0f, e => e.PurchasePrice.HasValue ? e.PurchasePrice.Value.ToString("N2", CultureInfo.GetCultureInfo("pl-PL")) : "",                    true),
                ("Zakup",      5.0f, e => e.PurchaseDate?.ToString("dd.MM.yyyy") ?? "",                                                                                  true),
                ("Gwarancja",  5.0f, e => e.WarrantyEndDate?.ToString("dd.MM.yyyy") ?? "",                                                                               true),
                ("Uwagi",     13.0f, e => e.Notes ?? "",                                                                                                                 true),
            };

            var columns = allColumns.Where(c => c.Include).ToArray();
            int colCount = columns.Length;

            // Grupowanie po kategorii (zachowana kolejność SortOrder)
            var groups = rows
                .GroupBy(e => e.Category?.Name ?? "")
                .OrderBy(g => rows.First(e => (e.Category?.Name ?? "") == g.Key).Category?.SortOrder ?? 0);

            return container =>
            {
                container.Table(table =>
                {
                    table.ColumnsDefinition(def =>
                    {
                        foreach (var (_, weight, _, _) in columns)
                            def.RelativeColumn(weight);
                    });

                    // Wiersz nagłówkowy tabeli
                    table.Header(header =>
                    {
                        foreach (var (h, _, _, _) in columns)
                            header.Cell().Element(HeaderCell).Text(h).Bold();
                    });

                    // Grupy kategorii
                    int rowIndex = 0;
                    foreach (var group in groups)
                    {
                        // Belka kategorii — zajmuje całą szerokość tabeli
                        table.Cell().ColumnSpan((uint)colCount)
                             .Element(CategoryHeaderCell)
                             .Text(group.Key)
                             .Bold().FontSize(8.5f).FontColor(Colors.White);

                        foreach (var eq in group)
                        {
                            var bg = RowBackground(eq, rowIndex++);
                            foreach (var (_, _, accessor, _) in columns)
                            {
                                table.Cell().Element(c => DataCell(c, bg))
                                     .Text(accessor(eq)).FontSize(7.5f);
                            }
                        }
                    }
                });
            };
        }

        // ── Komórki ─────────────────────────────────────────────────────────────

        private static IContainer HeaderCell(IContainer c) =>
            c.Background(Colors.Blue.Darken2)
             .Padding(3)
             .BorderBottom(0.5f)
             .BorderColor(Colors.Blue.Darken3);

        private static IContainer CategoryHeaderCell(IContainer c) =>
            c.Background(Colors.Grey.Darken2)
             .PaddingVertical(4)
             .PaddingHorizontal(6);

        private static IContainer DataCell(IContainer c, string bg) =>
            c.Background(bg)
             .Padding(2)
             .BorderBottom(0.3f)
             .BorderColor(Colors.Grey.Lighten2);

        private static string RowBackground(Equipment eq, int index)
        {
            // Status ma wyższy priorytet niż naprzemienne tło
            return eq.Status switch
            {
                EquipmentStatus.Damaged         => "#FFD580",
                EquipmentStatus.Disposed        => "#FFAAAA",
                EquipmentStatus.UnderMaintenance => "#B3D9FF",
                _                               => index % 2 == 0 ? Colors.White : "#F5F5F5",
            };
        }

        // ── Pomocniki ────────────────────────────────────────────────────────────

        private static string FormatIp(Equipment e)
        {
            if (string.IsNullOrWhiteSpace(e.IpAddress)) return "";
            // Pokaż sam adres (analogicznie do IpAddressDisplayConverter)
            return e.IpAddress;
        }
    }
}
