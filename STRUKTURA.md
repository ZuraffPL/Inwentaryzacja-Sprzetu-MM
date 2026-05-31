# Struktura projektu — Inwentaryzacja Sprzętu

## Drzewo plików

```
d:\Inwentaryzacja Sprzętu\
│
├── InwentaryzacjaSprzetu.csproj       # Projekt .NET 10 WPF
├── App.xaml / App.xaml.cs             # Punkt startowy, rejestracja DI, lifecycle
├── MainWindow.xaml / MainWindow.xaml.cs   # Główne okno (menu + toolbar + baner alertów)
├── README.md                          # Opis projektu i funkcji
├── INSTALACJA.md                      # Instrukcja instalacji
├── STRUKTURA.md                       # Ten plik
├── CLAUDE.MD                          # Konwencje projektu dla GitHub Copilot
├── db_script.sql                      # Pomocniczy skrypt inspekcji SQLite (nieautorytatywny)
├── migracje.bat                       # Skrót do dotnet ef migrations add
├── uruchom.bat / uruchom.ps1          # Skrypty uruchomieniowe (bat / PowerShell)
├── Czysc-Buildy.bat                   # Czyści foldery bin/ i obj/
├── Zbuduj-Installer.bat               # Buduje instalator przez Inno Setup
├── Zainstaluj.bat                     # Uruchamia gotowy instalator
├── Start.vbs                          # Cichy launcher (bez okna konsoli)
├── Publish-App.ps1                    # Skrypt publikacji (self-contained)
├── Setup-Install.ps1                  # Skrypt konfiguracji środowiska
├── installer.iss                      # Skrypt Inno Setup (definicja instalatora)
│
├── 📁 Properties/
│   └── AssemblyInfo.cs                # Wersja assembly — MUSI być zgodna z .csproj
│                                      #   (GenerateAssemblyInfo=false, pack URI WPF wymaga zgodności)
│
├── 📁 Models/                         # Modele EF Core — mapowane na tabele SQLite
│   ├── Equipment.cs                   # Sprzęt + enum EquipmentStatus
│   ├── Category.cs                    # Kategoria sprzętu (z kodem i sortowaniem)
│   ├── Location.cs                    # Lokalizacja / pawilon (z kodem, kodem kraju i kodem pawilonu)
│   ├── Department.cs                  # Dział wewnętrzny (np. Kasa, Magazyn)
│   ├── InventoryEvent.cs              # Zdarzenie inwentarzowe (z PurchasePrice, NewStatus, załączniki)
│   ├── ChangeLog.cs                   # Log zmian pól sprzętu
│   └── Alert.cs                       # Powiadomienie/alert (z datą wyzwolenia, zakresem, archiwum)
│
├── 📁 Database/                       # Warstwa dostępu do danych
│   ├── InventoryDbContext.cs          # EF Core DbContext, Fluent API, seed data
│   └── DesignTimeDbContextFactory.cs  # Fabryka dla CLI migracji (dotnet ef)
│
├── 📁 Migrations/                     # Migracje EF Core (generowane automatycznie)
│   ├── 20250915085702_InitialCreate.*
│   ├── 20250915202643_AddIpAddressAndLocationFields.*
│   ├── 20250916225009_AddCategorySortOrder.*
│   ├── 20250917092329_AddDepartmentModel.*
│   ├── 20250918194207_AddConnectedToEquipment.*
│   ├── 20260311184356_AddEventPurchasePriceAndNewStatus.*
│   ├── 20260311190737_AddEventPreviousStatus.*
│   ├── 20260311194904_AddDepartmentChangeFields.*
│   ├── 20260316063930_AddLocationCountryCode.*
│   ├── 20260408120000_AddEventStatus.*
│   ├── 20260415084028_AddLocationPavilionCode.*
│   ├── 20260429044855_AddInventoryNumberChangeFields.*
│   ├── 20260429050307_AddIpAddressChangeFields.*
│   ├── 20260429053218_AddFiscalNumberChangeFields.*
│   ├── 20260511203722_AddEventAttachments.*
│   ├── 20260529193706_AddNoInventoryNumber.*
│   ├── 20260531215114_AddAlerts.*
│   └── InventoryDbContextModelSnapshot.cs
│
├── 📁 Services/                       # Warstwa serwisów — logika biznesowa i CRUD
│   ├── IEquipmentService.cs           # Interfejs serwisu sprzętu
│   ├── EquipmentService.cs            # Implementacja: CRUD, generowanie nr inwent., eksport/import CSV
│   ├── IServiceInterfaces.cs          # Interfejsy: ICategoryService, ILocationService,
│   │                                  #   IDepartmentService, IInventoryEventService,
│   │                                  #   IEventAttachmentService, IChangeLogService
│   ├── ServiceImplementations.cs      # Implementacje: Category, Location, InventoryEvent,
│   │                                  #   ChangeLog
│   ├── DepartmentService.cs           # Implementacja IDepartmentService
│   ├── EventAttachmentService.cs      # Implementacja IEventAttachmentService (pliki załączników)
│   ├── AlertService.cs                # IAlertService + implementacja (CRUD + archiwizacja alertów)
│   ├── CsvEquipmentService.cs         # Statyczny serwis: eksport/import sprzętu do/z CSV
│   └── LoggingService.cs              # ILoggingService + LoggingService (plik debug_log.txt)
│
├── 📁 ViewModels/                     # ViewModele MVVM (CommunityToolkit.Mvvm)
│   ├── MainWindowViewModel.cs         # Główny VM: nawigacja, ładowanie danych, komendy,
│   │                                  #   alerty (kolekcje, timer 30 min, event AlertCountChanged)
│   ├── EquipmentEditViewModel.cs      # VM formularza edycji/dodawania sprzętu
│   ├── CategoryEditViewModel.cs       # VM formularza kategorii
│   ├── LocationEditViewModel.cs       # VM formularza lokalizacji
│   ├── DepartmentEditViewModel.cs     # VM formularza działu
│   ├── EventEditViewModel.cs          # VM formularza zdarzenia (sync z Equipment)
│   ├── EventAttachmentsViewModel.cs   # VM zarządzania załącznikami zdarzeń
│   ├── EventsViewModel.cs             # VM widoku listy zdarzeń
│   └── AlertEditViewModel.cs          # VM formularza dodawania/edycji alertu
│                                      #   (wybór zakresu: sprzęt / kategoria / ogólne)
│
├── 📁 Views/                          # Widoki WPF (UserControls + Dialogi)
│   ├── EquipmentView.xaml/.cs         # Lista sprzętu z zakładkami per pawilon
│   ├── EquipmentEditDialog.xaml/.cs   # Dialog dodawania/edycji sprzętu
│   ├── EquipmentEventsWindow.xaml/.cs # Okno historii zdarzeń danego sprzętu
│   ├── ConnectedEquipmentWindow.xaml/.cs  # Okno sprzętów podpiętych do komputera
│   ├── CategoriesView.xaml/.cs        # Lista kategorii
│   ├── CategoryEditDialog.xaml/.cs    # Dialog kategorii
│   ├── LocationsView.xaml/.cs         # Lista lokalizacji
│   ├── LocationEditDialog.xaml/.cs    # Dialog lokalizacji
│   ├── DepartmentsView.xaml/.cs       # Lista działów
│   ├── DepartmentEditDialog.xaml/.cs  # Dialog działu
│   ├── EventsView.xaml/.cs            # Lista zdarzeń (grupowana wg typu)
│   ├── EventEditDialog.xaml/.cs       # Dialog zdarzenia (z polami warunkowymi)
│   ├── EventAttachmentsDialog.xaml/.cs # Dialog zarządzania załącznikami
│   ├── AlertsView.xaml/.cs            # Lista powiadomień (aktywne + expander archiwum)
│   ├── AlertEditDialog.xaml/.cs       # Dialog dodawania/edycji powiadomienia
│   ├── SplashWindow.xaml/.cs          # Ekran powitalny (min. 5s, status migracji)
│   ├── ChangelogWindow.xaml/.cs       # Okno historii wersji
│   ├── UserManualWindow.xaml/.cs      # Instrukcja obsługi
│   └── AboutWindow.xaml/.cs           # Okno „O programie"
│
└── 📁 Helpers/
    ├── Converters.cs                  # Konwertery WPF: EnumToString, StatusToColor,
    │                                  #   EventTypeToPastelBackground, BoolToVisibility itp.
    └── AppPreferences.cs              # Preferencje użytkownika (JSON, AppData/Roaming)
```

## Statusy sprzętu

| Wartość enum | Wyświetlana nazwa |
|---|---|
| `Active` | Sprawny |
| `Inactive` | Rezerwa |
| `UnderMaintenance` | W konserwacji |
| `Damaged` | Zepsuty (uszkodzony) |
| `Disposed` | Zutylizowany (kasacja) |

## Typy zdarzeń

| Wartość enum | Wyświetlana nazwa | Specjalne pola |
|---|---|---|
| `Purchase` | Zakup | Cena zakupu → synchronizuje `Equipment.PurchasePrice` |
| `Transfer` | Przeniesienie | — |
| `Maintenance` | Konserwacja | — |
| `Repair` | Naprawa | — |
| `Disposal` | Utylizacja | — |
| `Audit` | Zmiana nr inwent. | — |
| `StatusChange` | Zmiana statusu | Nowy status → synchronizuje `Equipment.Status` |
| `Other` | Inne | — |

## Numeracja inwentarzowa

Format: `[KOD_KAT]-[KOD_LOK]-[ROK][NR_SEQ_3_CYFRY]`  
Przykład: `KOMP-W01-2026001`

Logika: `EquipmentService.GenerateInventoryNumberAsync()`

## System powiadomień (alertów)

Powiadomienie jest **wyzwolone** gdy `TriggerDate.Date <= DateTime.Today` i nie jest zarchiwizowane (`IsArchived = false`).

Wyzwolone powiadomienia wyświetlają się w trzech miejscach:

| Miejsce | Wygląd |
|---|---|
| Pomarańczowy baner pod toolbarem | Widoczny na każdym widoku; kliknięcie otwiera zakładkę Powiadomień |
| Czerwony badge na przycisku toolbar | `🔔 Powiadomienia [N]` — badge pojawia się tylko gdy N > 0 |
| DataGrid w AlertsView | Wyzwolone wiersze: tło `#FFF3CD` (amber), pogrubiona czcionka |
| Tooltip ikony tray | Zmienia się na „Inwentaryzacja Sprzętu — N aktywnych powiadomień" |

Timer `DispatcherTimer` (co 30 minut) odpytuje bazę i odświeża kolekcje automatycznie.

Zakres powiadomienia: **konkretny sprzęt** / **cała kategoria** / **ogólne** (bez przypisania).

## Pakiety NuGet

| Pakiet | Wersja | Cel |
|---|---|---|
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0 | ORM + driver SQLite |
| `Microsoft.EntityFrameworkCore.Tools` | 9.0 | Narzędzia CLI migracji |
| `CommunityToolkit.Mvvm` | 8.4 | `[ObservableProperty]`, `[RelayCommand]` |
| `Microsoft.Extensions.Hosting` | 10.0 | Lifecycle aplikacji, DI |
| `Microsoft.Extensions.DependencyInjection` | 10.0 | DI container |

## Baza danych

- Plik: `inventory.db` (SQLite, tworzony automatycznie w `%LOCALAPPDATA%\InwentaryzacjaSprzetu\`)
- Migracje: `dotnet ef migrations add NazwaMigracji`
- Snapshot: `Migrations/InventoryDbContextModelSnapshot.cs`
- Preferencje użytkownika: `%APPDATA%\InwentaryzacjaSprzetu\preferences.json`

## Historia migracji

| Migracja | Opis |
|---|---|
| `20250915085702_InitialCreate` | Tabele bazowe: Equipment, Category, Location |
| `20250915202643_AddIpAddressAndLocationFields` | Adres IP, pola lokalizacji w Equipment |
| `20250916225009_AddCategorySortOrder` | Pole SortOrder w Category |
| `20250917092329_AddDepartmentModel` | Tabela Department + relacja z Equipment |
| `20250918194207_AddConnectedToEquipment` | Relacja ConnectedToEquipmentId (np. drukarka → komputer) |
| `20260311184356_AddEventPurchasePriceAndNewStatus` | PurchasePrice i NewStatus w InventoryEvent |
| `20260311190737_AddEventPreviousStatus` | PreviousStatus w InventoryEvent |
| `20260311194904_AddDepartmentChangeFields` | Pola zmiany działu w InventoryEvent |
| `20260316063930_AddLocationCountryCode` | CountryCode w Location |
| `20260408120000_AddEventStatus` | Status w InventoryEvent |
| `20260415084028_AddLocationPavilionCode` | PavilionCode w Location |
| `20260429044855_AddInventoryNumberChangeFields` | Pola zmiany numeru inwentarzowego w InventoryEvent |
| `20260429050307_AddIpAddressChangeFields` | Pola zmiany adresu IP w InventoryEvent |
| `20260429053218_AddFiscalNumberChangeFields` | Pola zmiany numeru fiskalnego w InventoryEvent |
| `20260511203722_AddEventAttachments` | Tabela EventAttachment (załączniki do zdarzeń) |
| `20260529193706_AddNoInventoryNumber` | Flaga NoInventoryNumber w Equipment |
| `20260531215114_AddAlerts` | Tabela alerts (system powiadomień) |


## Drzewo plików

```
d:\Inwentaryzacja Sprzętu\
│
├── InwentaryzacjaSprzetu.csproj       # Projekt .NET 9 WPF
├── App.xaml / App.xaml.cs             # Punkt startowy, rejestracja DI, lifecycle
├── MainWindow.xaml / MainWindow.xaml.cs   # Główne okno (TabControl + paski)
├── README.md                          # Opis projektu i funkcji
├── INSTALACJA.md                      # Instrukcja instalacji
├── STRUKTURA.md                       # Ten plik
├── CLAUDE.MD                          # Konwencje projektu dla GitHub Copilot
├── db_script.sql                      # Pomocniczy skrypt inspekcji SQLite (nieautorytatywny)
├── migracje.bat                       # Skrót do dotnet ef migrations add
├── uruchom.bat / uruchom.ps1          # Skrypty uruchomieniowe (bat / PowerShell)
├── Czysc-Buildy.bat                   # Czyści foldery bin/ i obj/
├── Zbuduj-Installer.bat               # Buduje instalator przez Inno Setup
├── Zainstaluj.bat                     # Uruchamia gotowy instalator
├── Start.vbs                          # Cichy launcher (bez okna konsoli)
├── Publish-App.ps1                    # Skrypt publikacji (self-contained)
├── Setup-Install.ps1                  # Skrypt konfiguracji środowiska
├── installer.iss                      # Skrypt Inno Setup (definicja instalatora)
│
├── 📁 Properties/
│   └── AssemblyInfo.cs                # Wersja assembly — MUSI być zgodna z .csproj
│                                      #   (GenerateAssemblyInfo=false, pack URI WPF wymaga zgodności)
│
├── 📁 Models/                         # Modele EF Core — mapowane na tabele SQLite
│   ├── Equipment.cs                   # Sprzęt + enum EquipmentStatus
│   ├── Category.cs                    # Kategoria sprzętu (z kodem i sortowaniem)
│   ├── Location.cs                    # Lokalizacja / pawilon (z kodem i kodem kraju)
│   ├── Department.cs                  # Dział wewnętrzny (np. Kasa, Magazyn)
│   ├── InventoryEvent.cs              # Zdarzenie inwentarzowe (z PurchasePrice, NewStatus)
│   └── ChangeLog.cs                   # Log zmian pól sprzętu
│
├── 📁 Database/                       # Warstwa dostępu do danych
│   ├── InventoryDbContext.cs          # EF Core DbContext, Fluent API, seed data
│   └── DesignTimeDbContextFactory.cs  # Fabryka dla CLI migracji (dotnet ef)
│
├── 📁 Migrations/                     # Migracje EF Core (generowane automatycznie)
│   ├── 20250915085702_InitialCreate.*
│   ├── 20250915202643_AddIpAddressAndLocationFields.*
│   ├── 20250916225009_AddCategorySortOrder.*
│   ├── 20250917092329_AddDepartmentModel.*
│   ├── 20250918194207_AddConnectedToEquipment.*
│   ├── 20260311184356_AddEventPurchasePriceAndNewStatus.*
│   ├── 20260311190737_AddEventPreviousStatus.*
│   ├── 20260311194904_AddDepartmentChangeFields.*
│   ├── 20260316063930_AddLocationCountryCode.*
│   └── InventoryDbContextModelSnapshot.cs
│
├── 📁 Services/                       # Warstwa serwisów — logika biznesowa i CRUD
│   ├── IEquipmentService.cs           # Interfejs serwisu sprzętu
│   ├── EquipmentService.cs            # Implementacja: CRUD, generowanie nr inwent.
│   ├── IServiceInterfaces.cs          # Interfejsy: ICategoryService, ILocationService,
│   │                                  #   IDepartmentService, IInventoryEventService,
│   │                                  #   IChangeLogService
│   ├── ServiceImplementations.cs      # Implementacje: Category, Location, InventoryEvent,
│   │                                  #   ChangeLog
│   ├── DepartmentService.cs           # Implementacja IDepartmentService
│   └── LoggingService.cs              # ILoggingService + LoggingService (plik debug_log.txt)
│
├── 📁 ViewModels/                     # ViewModele MVVM (CommunityToolkit.Mvvm)
│   ├── MainWindowViewModel.cs         # Główny VM: tabs, ładowanie danych, komendy
│   ├── EquipmentEditViewModel.cs      # VM formularza edycji/dodawania sprzętu
│   ├── CategoryEditViewModel.cs       # VM formularza kategorii
│   ├── LocationEditViewModel.cs       # VM formularza lokalizacji
│   ├── DepartmentEditViewModel.cs     # VM formularza działu
│   ├── EventEditViewModel.cs          # VM formularza zdarzenia (sync z Equipment)
│   └── EventsViewModel.cs             # VM widoku listy zdarzeń
│
├── 📁 Views/                          # Widoki WPF (UserControls + Dialogi)
│   ├── EquipmentView.xaml/.cs         # Lista sprzętu z zakładkami per pawilon
│   ├── EquipmentEditDialog.xaml/.cs   # Dialog dodawania/edycji sprzętu
│   ├── EquipmentEventsWindow.xaml/.cs # Okno historii zdarzeń danego sprzętu
│   ├── ConnectedEquipmentWindow.xaml/.cs  # Okno sprzętów podpiętych do komputera
│   ├── CategoriesView.xaml/.cs        # Lista kategorii
│   ├── CategoryEditDialog.xaml/.cs    # Dialog kategorii
│   ├── LocationsView.xaml/.cs         # Lista lokalizacji
│   ├── LocationEditDialog.xaml/.cs    # Dialog lokalizacji
│   ├── DepartmentsView.xaml/.cs       # Lista działów
│   ├── DepartmentEditDialog.xaml/.cs  # Dialog działu
│   ├── EventsView.xaml/.cs            # Lista zdarzeń (grupowana wg typu)
│   ├── EventEditDialog.xaml/.cs       # Dialog zdarzenia (z polami warunkowymi)
│   └── SplashWindow.xaml/.cs          # Ekran powitalny (min. 5s, status migracji)
│
└── 📁 Helpers/
    ├── Converters.cs                  # Konwertery WPF: EnumToString, StatusToColor,
    │                                  #   EventTypeToPastelBackground, BoolToVisibility itp.
    └── AppPreferences.cs              # Preferencje użytkownika (JSON, AppData/Roaming)
```

## Statusy sprzętu

| Wartość enum | Wyświetlana nazwa |
|---|---|
| `Active` | Sprawny |
| `Inactive` | Rezerwa |
| `UnderMaintenance` | W konserwacji |
| `Damaged` | Zepsuty (uszkodzony) |
| `Disposed` | Zutylizowany (kasacja) |

## Typy zdarzeń

| Wartość enum | Wyświetlana nazwa | Specjalne pola |
|---|---|---|
| `Purchase` | Zakup | Cena zakupu → synchronizuje `Equipment.PurchasePrice` |
| `Transfer` | Przeniesienie | — |
| `Maintenance` | Konserwacja | — |
| `Repair` | Naprawa | — |
| `Disposal` | Utylizacja | — |
| `Audit` | Zmiana nr inwent. | — |
| `StatusChange` | Zmiana statusu | Nowy status → synchronizuje `Equipment.Status` |
| `Other` | Inne | — |

## Numeracja inwentarzowa

Format: `[KOD_KAT]-[KOD_LOK]-[ROK][NR_SEQ_3_CYFRY]`  
Przykład: `KOMP-W01-2026001`

Logika: `EquipmentService.GenerateInventoryNumberAsync()`

## Pakiety NuGet

| Pakiet | Wersja | Cel |
|---|---|---|
| `Microsoft.EntityFrameworkCore.Sqlite` | 9.0 | ORM + driver SQLite |
| `Microsoft.EntityFrameworkCore.Tools` | 9.0 | Narzędzia CLI migracji |
| `CommunityToolkit.Mvvm` | 8.3.2 | `[ObservableProperty]`, `[RelayCommand]` |
| `Microsoft.Extensions.Hosting` | 9.0 | Lifecycle aplikacji, DI |
| `Microsoft.Extensions.DependencyInjection` | 9.0 | DI container |

## Baza danych

- Plik: `inventory.db` (SQLite, tworzony automatycznie w `%LOCALAPPDATA%\InwentaryzacjaSprzetu\`)
- Migracje: `dotnet ef migrations add NazwaMigracji`
- Snapshot: `Migrations/InventoryDbContextModelSnapshot.cs`
- Preferencje użytkownika: `%APPDATA%\InwentaryzacjaSprzetu\preferences.json`

## Historia migracji

| Migracja | Opis |
|---|---|
| `20250915085702_InitialCreate` | Tabele bazowe: Equipment, Category, Location |
| `20250915202643_AddIpAddressAndLocationFields` | Adres IP, pola lokalizacji w Equipment |
| `20250916225009_AddCategorySortOrder` | Pole SortOrder w Category |
| `20250917092329_AddDepartmentModel` | Tabela Department + relacja z Equipment |
| `20250918194207_AddConnectedToEquipment` | Relacja ConnectedToEquipmentId (np. drukarka → komputer) |
| `20260311184356_AddEventPurchasePriceAndNewStatus` | PurchasePrice i NewStatus w InventoryEvent |
| `20260311190737_AddEventPreviousStatus` | PreviousStatus w InventoryEvent |
| `20260311194904_AddDepartmentChangeFields` | Pola zmiany działu w InventoryEvent |
| `20260316063930_AddLocationCountryCode` | CountryCode w Location |
