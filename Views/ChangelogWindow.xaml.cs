using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using InwentaryzacjaSprzetu.Helpers;

namespace InwentaryzacjaSprzetu.Views
{
    public partial class ChangelogWindow : Window
    {
        private readonly AppPreferences _prefs;
        private readonly string _currentVersionString;

        public List<ChangelogEntry> Entries { get; }

        public bool DontShowAgain { get; set; }

        public ChangelogWindow(AppPreferences prefs)
        {
            _prefs = prefs;

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            _currentVersionString = $"{version?.Major}.{version?.Minor}.{version?.Build}";

            Entries = BuildChangelog();
            DontShowAgain = prefs.ChangelogDismissedVersion == _currentVersionString;

            InitializeComponent();
            DataContext = this;
        }

        private List<ChangelogEntry> BuildChangelog() => new()
        {
            new ChangelogEntry
            {
                Version = "v1.9.6",
                Date = "czerwiec 2026",
                IsLatest = true,
                Changes = new()
                {
                    "System powiadomień (alertów) — tworzenie powiadomień z datą wyzwolenia przypisanych do konkretnego sprzętu, kategorii lub ogólnych",
                    "Powiadomienia wyświetlane jako pomarańczowy baner pod paskiem narzędzi — kliknięcie przenosi do widoku Powiadomień",
                    "Przycisk Powiadomienia w pasku narzędzi z czerwonym licznikiem aktywnych alertów",
                    "Okno startowe przy uruchomieniu — lista aktywnych powiadomień z kartami, opcja 'Nie pokazuj dziś ponownie'",
                    "Widok Powiadomień: lista aktywnych (żółte wiersze dla wyzwolonych) + rozwijany panel archiwum",
                    "Funkcje: Dodaj / Edytuj / Archiwizuj / Usuń powiadomienie przez toolbar widoku",
                    "Timer co 30 minut automatycznie odświeża listę powiadomień",
                    "Minimalizacja do zasobnika systemowego (tray) — okno znika z paska zadań, aplikacja działa w tle",
                    "Ikona traya z dzwonkiem — zmienia się na czerwoną z '!' gdy są aktywne powiadomienia",
                    "Eksport i import sprzętu do/z pliku CSV (menu Plik → Eksport do CSV / Import z CSV)",
                    "CSV: separator ;, kodowanie UTF-8 z BOM (kompatybilny z polskim Excel), obsługa cudzysłowów RFC 4180",
                }
            },
            new ChangelogEntry
            {
                Version = "v1.9.4",
                Date = "maj 2026",
                IsLatest = false,
                Changes = new()
                {
                    "Obsługa sprzętu bez numeru inwentarzowego — checkbox 'Brak nr inwentarzowego' w oknie dodawania/edycji sprzętu",
                    "Sprzęt bez numeru oznaczony pomarańczowym kółkiem z symbolem ? w kolumnie Nr Inwentarzowy na liście sprzętu",
                    "Nowe zdarzenie 'Nadanie nr inwentarzowego' — przypisanie numeru do sprzętu bez numeru; numer generowany automatycznie wg formatu kategorii i pawilonu",
                    "Zdarzenie 'Nadanie nr inwentarzowego' dostępne wyłącznie dla sprzętu bez przypisanego numeru",
                    "Tytuł okna dodawania sprzętu zmieniony na 'Dodaj sprzęt' (wcześniej wyświetlał 'Edycja sprzętu')",
                    "Naprawiono błędną ścieżkę do exe w skryptach uruchom.bat i uruchom.ps1 (net10.0-windows → net10.0-windows7.0)",
                }
            },
            new ChangelogEntry
            {
                Version = "v1.9.3",
                Date = "maj 2026",
                IsLatest = false,
                Changes = new()
                {
                    "Załączniki do zdarzeń — możliwość podpinania plików JPG, PNG, PDF, WEBP do dowolnego zdarzenia",
                    "Dodawanie załączników: z dialogu edycji zdarzenia, przez menu kontekstowe (PPM → Dodaj załącznik), skrótem Ctrl+Z na zaznaczonym zdarzeniu",
                    "Przeglądanie załączników: okno z listą plików, otwieranie w domyślnej aplikacji (PPM → Pokaż załączniki [Z] lub klawisz Z)",
                    "Aplikacja pyta o folder przechowywania załączników przy pierwszym użyciu i zapamiętuje go w preferencjach",
                    "Wizualna indykacja 📎 w widoku zdarzeń — ikona spinacza obok każdego zdarzenia posiadającego załączniki",
                    "Eksport załączników jako archiwum ZIP (Narzędzia → Eksportuj załączniki jako ZIP) z manifestem oryginalnej ścieżki",
                    "Import załączników z ZIP (Narzędzia → Importuj załączniki z ZIP) — pyta o oryginalną lub nową ścieżkę ekstrakcji",
                    "Zwijanie / rozwijanie sekcji Aktywne zdarzenia i Archiwum zdarzeń przez kliknięcie w belkę nagłówkową",
                    "Migracja do .NET 10 — aktualizacja wszystkich pakietów do wersji 10.0.7",
                }
            },
            new ChangelogEntry
            {
                Version = "v1.8.8",
                Date = "kwiecień 2026",
                IsLatest = false,
                Changes = new()
                {
                    "Filtrowanie po kategorii sprzętu (multi-select) — możliwość zaznaczenia wielu kategorii jednocześnie, tak jak istniejące filtry działu i statusu",
                    "Stan filtra kategorii zapisywany w preferencjach i przywracany po restarcie aplikacji",
                    "Naprawiono błąd utraty zaznaczonych filtrów przy związywaniu/rozwijaniu grup lub zmianie widoczności krajów (code-behind nadpisywał plik preferencji starzą kopią)",
                    "W oknie dodawania/edycji sprzętu lista działów zawiera pozycję „N/A (brak działu)“ na początku",
                    "Nowy sprzęt ma domyślnie ustawiony brak działu (N/A) zamiast pustego pola",
                }
            },
            new ChangelogEntry
            {
                Version = "v1.8.6",
                Date = "kwiecień 2026",
                IsLatest = false,
                Changes = new()
                {
                    "Filtry wielokrotnego wyboru po dziale i statusie w widoku sprzętu (zaznacz kilka działów lub statusów jednocześnie)",
                    "Stan filtrów zapisywany w preferencjach i przywracany automatycznie po restarcie aplikacji",
                    "Eksport widoku sprzętu do PDF (A4 landscape) z uwzględnieniem aktywnych filtrów",
                    "PDF: lokalizacja wyświetlana w tytule raportu, kategorie jako belki rozdzielające sekcje",
                    "PDF: dynamiczne ukrywanie kolumny Dział (gdy filtr po dziale aktywny) i Status (gdy filtr po statusie aktywny)",
                }
            },
            new ChangelogEntry
            {
                Version = "v1.8.2",
                Date = "kwiecień 2026",
                IsLatest = false,
                Changes = new()
                {
                    "Nowe zdarzenie: Zmiana nr unikatowego kasy fiskalnej — auto-wypełnia poprzedni nr z karty sprzętu i aktualizuje go po zapisaniu",
                    "Telefony komórkowe (TK): pole Nr seryjny zastąpione etykietą IMEI w oknie edycji sprzętu",
                    "Nagłówek grupy kategorii Telefony komórkowe w widoku sprzętu wyświetla teraz: Marka | Model | IMEI",
                    "Klawisz A na zaznaczonym zdarzeniu aktywnym przenosi je do archiwum",
                    "Liczba aktywnych zdarzeń widoczna w nagłówku grupy kategorii w widoku sprzętu",
                }
            },
            new ChangelogEntry
            {
                Version = "v1.6.7",
                Date = "kwiecień 2026",
                IsLatest = false,
                Changes = new()
                {
                    "Dynamiczne nagłówki kolumn dla kategorii Monitory: Przekątna zamiast Nr Seryjny",
                    "Dynamiczne nagłówki kolumn dla kategorii Kasy Fiskalne: Nr unikatowy zamiast Nr Seryjny",
                    "Etykiety specyficzne dla kategorii wyświetlane w belce grupowania listy sprzętu",
                    "Dynamiczne wyrównanie etykiet w belce grupy do rzeczywistej pozycji kolumny",
                }
            },
            new ChangelogEntry
            {
                Version = "v1.6.3",
                Date = "kwiecień 2026",
                IsLatest = false,
                Changes = new()
                {
                    "Dodano okno Instrukcja obsługi dostępne z menu Pomoc — 11 sekcji tematycznych z nawigacją",
                    "Przeprojektowano okno O programie — spójny wygląd z resztą aplikacji, widoczny autor",
                }
            },
            new ChangelogEntry
            {
                Version = "v1.6.2",
                Date = "kwiecień 2026",
                IsLatest = false,
                Changes = new()
                {
                    "Dodano kategorię Laptopy z dedykowanym formatem numeru inwentarzowego (nr/KodKraju/KodPawilonu, np. 1/PL/ZM)",
                    "Dodano pole Kod Pawilonu do zarządzania lokalizacjami (pawilon widoczny w liście i oknie edycji)",
                    "Poprawiono kody kategorii: KK (Czytniki kodów kreskowych), AP (Router/Switch), TE (Testery Banknotów), SC (Sprawdzarki cen)",
                    "Dodana historia wersji z automatycznym pokazywaniem przy uruchomieniu po aktualizacji",
                }
            },
            new ChangelogEntry
            {
                Version = "v1.5.3",
                Date = "marzec 2026",
                IsLatest = false,
                Changes = new()
                {
                    "Dodano możliwość kopiowania danych z istniejącego sprzętu przy dodawaniu nowego",
                    "Przebudowano widok Zdarzeń — sekcje aktywne/archiwalne z przełącznikami grup",
                    "Dodano kolorowanie wierszy: fioletowy dla działu Rezerwa (REZ), niebieski dla konserwacji",
                    "Rozbudowano pasek stanu o liczniki statusów i rezerwę sprzętu",
                    "Dodano obsługę kategorii Kolektory Danych (KD) z podpięciem do komputera",
                }
            },
            new ChangelogEntry
            {
                Version = "v1.5.0",
                Date = "wrzesień 2025",
                IsLatest = false,
                Changes = new()
                {
                    "Pierwsza wersja produkcyjna aplikacji",
                    "Zarządzanie sprzętem: dodawanie, edycja, usuwanie z podziałem na kategorie i lokalizacje",
                    "Pełna historia zdarzeń sprzętu (zakup, serwis, przeniesienie, kasacja)",
                    "Zarządzanie kategoriami, lokalizacjami i działami",
                    "Eksport i import bazy danych SQLite",
                }
            },
        };

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _prefs.ChangelogDismissedVersion = DontShowAgain ? _currentVersionString : null;
            _prefs.Save();
            base.OnClosing(e);
        }
    }

    public class ChangelogEntry
    {
        public string Version { get; set; } = "";
        public string Date { get; set; } = "";
        public bool IsLatest { get; set; }
        public List<string> Changes { get; set; } = new();
    }
}
