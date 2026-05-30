# Inwentaryzacja Sprzętu

Aplikacja desktopowa WPF do zarządzania bazą sprzętu rozmieszczonego na pawilonach sieci sklepów. Umożliwia przeglądanie, dodawanie, edycję i usuwanie rekordów sprzętowych per lokalizacja, śledzenie historii zdarzeń oraz synchronizację statusów.

## Wymagania systemowe

- Windows 10 lub nowszy (x64)
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (do budowania)
- lub .NET 10.0 Runtime (tylko do uruchomienia gotowego buildu)

## Uruchomienie

```powershell
# Sklonuj/pobierz projekt, następnie:
cd "d:\Inwentaryzacja Sprzętu"

dotnet restore
dotnet build
dotnet run
```

Lub użyj dołączonego skryptu:

```powershell
.\uruchom.ps1
```

Przy pierwszym starcie aplikacja automatycznie tworzy bazę SQLite `inventory.db` i uruchamia migracje.

## Funkcjonalności

### Sprzęt
- Automatyczna numeracja inwentarzowa: `[KOD_KAT]-[KOD_LOK]-[ROK][NR]`, np. `KOMP-W01-2026001`
- Zakładki per pawilon — szybki przegląd sprzętu w danej lokalizacji
- Filtrowanie przez pole wyszukiwania (nr inwentarzowy, nazwa, marka, model, nr seryjny)
- **Filtry wielokrotnego wyboru** — filtrowanie po dziale, statusie i **kategorii sprzętu** z możliwością zaznaczenia kilku opcji jednocześnie; stan filtrów zapamiêtywany miêdzy sesjami
- Statusy: **Sprawny**, **Rezerwa**, **W konserwacji**, **Zepsuty (uszkodzony)**, **Zutylizowany (kasacja)**
- Kolorowanie wierszy według statusu
- Pola: marka, model, nr seryjny / IMEI (telefony) / procesor (komputery) / nr unikatowy (kasy fiskalne), adres IP, dział, cena zakupu, data zakupu, data końca gwarancji
- Dynamiczne etykiety pól i nagłówków kolumn zależne od kategorii sprzętu (Marka | Model | IMEI dla telefonów, Marka | Model | Nr unikatowy dla kas fiskalnych itp.)
- Liczba aktywnych zdarzeń widoczna w nagłówkach grup kategorii w widoku sprzętu
- **Eksport do PDF** — eksport widoku sprzętu (z uwzględnieniem aktywnych filtrów) do pliku PDF w układzie poziomym (A4 landscape); kategorie jako sekcje rozdzielające, lokalizacja w tytule raportu, dynamiczne ukrywanie kolumn Dział/Status gdy filtr aktywny
- Menu kontekstowe (PPM): Edytuj / Dodaj zdarzenie / Usuń

### Kategorie i Lokalizacje
- Definicja własnych kategorii (z kodem, np. `KOMP`) i sortowaniem
- Definicja własnych lokalizacji (z kodem, np. `W01`) i opisem
- Kody używane w generowaniu numeru inwentarzowego

### Działy
- Przypisywanie sprzętu do działu wewnętrznego (np. Kasa, Magazyn, Biuro)
- Pole „Gdzie?" do dokładniejszego określenia umiejscowienia

### Zdarzenia (historia)
- Typy: Zakup, Przeniesienie, Konserwacja, Naprawa, Utylizacja, Zmiana nr inwent., Zmiana statusu, Zmiana działu, Zmiana adresu IP, Zmiana nr unikatowego kasy fiskalnej, Inne
- Grupowanie zdarzeń według typu (sekcje zwijane) z kolorowymi nagłówkami
- **Zakup** — pole ceny zakupu, automatyczny zapis do karty sprzętu
- **Zmiana statusu** — wybór nowego statusu, automatyczna aktualizacja statusu sprzętu
- **Zmiana działu** — wybór nowego działu, automatyczna aktualizacja przypisania
- **Zmiana nr inwentarzowego** — auto-wypełnia poprzedni nr, aktualizuje sprzęt po zapisaniu
- **Zmiana adresu IP** — auto-wypełnia poprzedni IP, aktualizuje sprzęt po zapisaniu
- **Zmiana nr unikatowego kasy fiskalnej** — auto-wypełnia poprzedni nr unikatowy kasy, aktualizuje sprzęt po zapisaniu
- Archiwizacja zdarzenia klawiszem **A** na zaznaczonej pozycji
- Dodawanie zdarzenia bezpośrednio z kontekstu sprzętu (pre-populate danych)
- Pole wykonującego, notatki, data

### Interfejs
- Ekran powitalny (splash screen) z animowanym ładowaniem
- Pasek statusu z informacjami o wybranym sprzęcie
- Zmiana szerokości kolumn (auto-width)

## Stack technologiczny

| Element | Wersja |
|---|---|
| .NET / C# | 10.0 |
| WPF (XAML) | net10.0-windows |
| Entity Framework Core | 10.0 |
| SQLite | via EF Core |
| CommunityToolkit.Mvvm | 8.4.2 |
| Microsoft.Extensions.Hosting | 10.0 |
| QuestPDF | 2026.5.0 |

## Architektura

Wzorzec **MVVM**: modele danych (`Models/`), warstwa serwisów z interfejsami (`Services/`), ViewModele (`ViewModels/`), widoki XAML (`Views/`). DI przez `Microsoft.Extensions.Hosting`.

## Baza danych

Plik `inventory.db` w katalogu projektu. Schemat zarządzany migracjami EF Core (folder `Migrations/`). Przy każdym starcie wykonywane jest `MigrateAsync()`.

```powershell
# Backup bazy:
Copy-Item "inventory.db" "inventory_backup_$(Get-Date -Format 'yyyyMMdd').db"
```

## Rozwijanie projektu

```powershell
# Nowa migracja EF Core po zmianie modeli:
dotnet ef migrations add NazwaMigracji

# Uruchomienie po zmianach:
dotnet run
```

Szczegóły konwencji, zasad w projekcie i decyzji architektonicznych: [CLAUDE.MD](CLAUDE.MD)

Struktura plików projektu: [STRUKTURA.md](STRUKTURA.md)

## Licencja

[Creative Commons Attribution 4.0 International (CC BY 4.0)](LICENSE)

## Historia wersji

### v1.9.4
- Obsługa sprzętu bez numeru inwentarzowego — checkbox „Brak nr inwentarzowego" w oknie dodawania/edycji sprzętu
- Sprzęt bez numeru oznaczony pomarańczowym kółkiem z „?" w kolumnie Nr Inwentarzowy na liście
- Nowe zdarzenie „Nadanie nr inwentarzowego" — przypisanie numeru do sprzętu bez numeru z auto-generacją wg formatu kategorii/pawilonu
- Poprawiono tytuł okna dodawania sprzętu (było: „Edycja sprzętu", powinno: „Dodaj sprzęt")
- Naprawiono ścieżkę exe w `uruchom.bat` i `uruchom.ps1` po zmianie target framework

### v1.8.8
- Filtrowanie po kategorii sprzętu (multi-select) — obok istniejących filtrów działu i statusu
- Stan filtra kategorii zapisywany w preferencjach i przywracany po restarcie
- Naprawiono błąd: zwinięcie/rozwijanie grup lub zmiana widoczności krajów nadpisywały filtry w preferencjach
- Lista działów w oknie edycji sprzętu zawiera pozycję „N/A (brak działu)“ na początku listy

### v1.8.6
- Filtry wielokrotnego wyboru po dziale i statusie (ToggleButton + Popup + CheckBox) w widoku sprzętu
- Stan filtrów zapisywany w preferencjach i przywracany po restarcie aplikacji
- Eksport widoku sprzętu do PDF (A4 landscape) via QuestPDF:
  - Lokalizacja wyświetlana w tytule raportu
  - Kategorie jako belki rozdzielające (separator rows) zamiast osobnej kolumny
  - Dynamiczne ukrywanie kolumny Dział (gdy aktywny filtr po dziale) i Status (gdy aktywny filtr po statusie)
  - Kolorowanie wierszy wg statusu, stopka ze stronicowaniem i datą generowania

### v1.8.2
- Powiadomienia o zdarzeniach i archiwizacja klawiszem A
- Zmiana nr unikatowego kasy fiskalnej jako typ zdarzenia

### v1.8.0
- Ekran powitalny (splash screen) z animowanym ładowaniem
- Zwijanie/rozwijanie sekcji zdarzeń per typ

### v1.7.x i wcześniejsze
- Podstawowe funkcje CRUD sprzętu, kategorii, lokalizacji, działów i zdarzeń
- System numeracji inwentarzowej, zakładki pawilonów, filtrowanie po kraju
- Historia zdarzeń z grupowaniem wg pawilonu

