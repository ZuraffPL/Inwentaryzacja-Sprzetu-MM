# CLAUDE.md — Inwentaryzacja Sprzętu

## Kontekst projektu
Aplikacja desktopowa do zarządzania bazą sprzętu rozmieszczonego na pawilonach sieci sklepów.
Cel: przeglądanie, dodawanie, edycja i usuwanie rekordów sprzętowych per lokalizacja.

## Stack technologiczny
- **Język:** C# (.NET 10+)
- **UI Framework:** WPF (Windows Presentation Foundation)
- **Markup UI:** XAML
- **Baza danych:** SQLite 3 via Entity Framework Core (`Microsoft.EntityFrameworkCore.Sqlite`)
- **ORM:** Entity Framework Core 9 z migracjami
- **DI / Hosting:** `Microsoft.Extensions.Hosting` + `Microsoft.Extensions.DependencyInjection`
- **IDE:** VS Code + GitHub Copilot (Claude Sonnet/Opus)

## Architektura projektu
- Wzorzec **MVVM** (Model-View-ViewModel) — obowiązkowy dla WPF
- Warstwa danych oddzielona od logiki UI (foldery `Models/`, `ViewModels/`, `Views/`, `Services/`, `Database/`)
- Dostęp do bazy przez **Service Pattern** — Services implementują interfejsy (`IEquipmentService`, `ICategoryService` itp.), ViewModele korzystają wyłącznie z interfejsów
- Połączenie z SQLite tylko przez `Database/InventoryDbContext.cs` (EF Core DbContext)
- Schemat bazy zarządzany przez **migracje EF Core** (folder `Migrations/`), nie ręczny SQL

## Konwencje nazewnicze
- Klasy: `PascalCase` (np. `EquipmentViewModel`, `PavilionRepository`)
- Właściwości i pola publiczne: `PascalCase`
- Pola prywatne: `_camelCase`
- Pliki XAML: nazwa = nazwa klasy (np. `EquipmentView.xaml` + `EquipmentView.xaml.cs`)
- Tabele SQLite: `snake_case` (np. `equipment_items`, `pavilions`)

## Reguły XAML / WPF
- Logika **tylko** w ViewModelu, nie w code-behind (`*.xaml.cs`)
- Używaj `INotifyPropertyChanged` lub dziedzicz po `ObservableObject`
- Bindowania zawsze przez `{Binding PropertyName}` — nie manipuluj UI z C# bezpośrednio
- Używaj `ICommand` (np. `RelayCommand`) zamiast event handlerów w code-behind
- Style i szablony kontrolek w osobnych plikach ResourceDictionary (`Styles/`)

## Baza danych SQLite
- Schemat bazy zarządzany przez **migracje EF Core** — każda zmiana przez `dotnet ef migrations add <Nazwa>`
- Migracjom towarzyszą testy kompilacji — zawsze uruchom `dotnet build` po migracji
- EF Core zawsze używa parametryzowanych zapytań — nie pisz ręcznego SQL w LINQ
- Do złożonych raportów/filtrów używaj LINQ, a nie surowego SQL
- Transakcje przez `DbContext.Database.BeginTransactionAsync()` przy operacjach wieloetapowych
- Indeksy konfiguruje się w `OnModelCreating()` przez EF Fluent API
- Plik `db_script.sql` w katalogu głównym — pomocniczy skrypt do inspekcji, nieautorytatywny

## Biblioteki — zasady
- Nie dodawaj bibliotek bez potrzeby, ale **aktywnie proponuj** nowe paczki NuGet, jeśli upraszczają kod lub rozwiązują problem lepiej
- Aktualnie używane biblioteki:
  - `Microsoft.EntityFrameworkCore.Sqlite` — ORM + driver SQLite (v10.0)
  - `Microsoft.EntityFrameworkCore.Tools` — narzędzia CLI do migracji
  - `CommunityToolkit.Mvvm` — MVVM helpers: `[ObservableProperty]`, `[RelayCommand]`, `ObservableObject`
  - `Microsoft.Extensions.Hosting` — IHost, lifecycle aplikacji
  - `Microsoft.Extensions.DependencyInjection` — DI container
- Dopuszczalne w przyszłości:
  - `Serilog` — logowanie zdarzeń/błędów (zastąpiłoby `LoggingService.cs`)
  - `xUnit` lub `NUnit` — testy jednostkowe
- Przy każdej propozycji nowej biblioteki podaj: nazwę NuGet, powód użycia, alternatywę

## Obsługa błędów
- Wszystkie operacje I/O i DB owijaj w `try/catch`
- Błędy loguj przez `ILoggingService` (plik `debug_log.txt`, 5 MB limit, najnowsze na górze)
- Użytkownik powinien widzieć komunikat (MessageBox lub status bar) — nie cicha awaria
- Puste `catch { }` jest zakazane — **wyjątek:** bloki „firewall" chroniące serwis logowania przed nieskończoną pętlą (np. błąd w `TrimOldest()`) oraz ostatnia linia obrony w `LogUnhandledException` — zawsze skomentuj dlaczego

## Zachowanie AI (GitHub Copilot / Claude)

### Zadawaj pytania doprecyzowujące, gdy:
- Wymaganie biznesowe jest nieprecyzyjne (np. "dodaj filtrowanie" — po czym filtrować?)
- Istnieją dwa równoważne podejścia architektoniczne i wybór wpłynie na strukturę projektu
- Nie jesteś pewien, czy zmiana dotknie innych modułów
- Schemat bazy danych wymaga decyzji projektowej (relacje, typy danych, indeksy)

### Proponuj zmiany bibliotek, gdy:
- Aktualna implementacja jest zbyt skomplikowana bez zewnętrznej pomocy
- Istnieje paczka NuGet rozwiązująca problem stabilnie i popularnie
- Zauważysz, że piszemy "ręcznie" coś, co biblioteka robi lepiej
- Zawsze uzasadnij propozycję i podaj alternatywę

### Ogólne zasady pracy:
- Przed większą zmianą opisz plan w punktach i czekaj na akceptację
- Nie refaktoryzuj kodu poza zakresem zadania bez pytania
- Jeśli zadanie jest duże — podziel na kroki i realizuj etapami
- Kod musi się kompilować po każdym kroku
- Komentarze w kodzie po polsku lub angielsku — konsekwentnie

## Struktura folderów (aktualna)
```
InwentaryzacjaSprzetu/
├── Database/
│   ├── InventoryDbContext.cs          ← EF Core DbContext, konfiguracja modelu, seed data
│   └── DesignTimeDbContextFactory.cs  ← fabryka dla CLI migracji
├── Migrations/                         ← migracje EF Core (generowane automatycznie)
├── Models/
│   ├── Equipment.cs
│   ├── Category.cs
│   ├── Location.cs
│   ├── Department.cs
│   ├── InventoryEvent.cs
│   └── ChangeLog.cs
├── Services/
│   ├── IServiceInterfaces.cs           ← interfejsy: ICategoryService, ILocationService itp.
│   ├── IEquipmentService.cs            ← interfejs IEquipmentService
│   ├── EquipmentService.cs
│   ├── ServiceImplementations.cs       ← CategoryService, LocationService itp.
│   ├── DepartmentService.cs
│   └── LoggingService.cs               ← ILoggingService + LoggingService
├── ViewModels/
│   ├── MainWindowViewModel.cs
│   ├── EquipmentEditViewModel.cs
│   ├── CategoryEditViewModel.cs
│   ├── LocationEditViewModel.cs
│   ├── DepartmentEditViewModel.cs
│   ├── EventEditViewModel.cs
│   ├── EventsViewModel.cs
│   └── ViewModelExtensions.cs
├── Views/
│   ├── MainWindow.xaml
│   ├── SplashWindow.xaml               ← ekran powitalny
│   ├── EquipmentView.xaml
│   ├── EquipmentEditDialog.xaml
│   ├── CategoriesView.xaml
│   ├── LocationsView.xaml
│   ├── DepartmentsView.xaml
│   └── EventsView.xaml  (+ odpowiedniki *Dialog.xaml)
├── Helpers/
│   └── Converters.cs                   ← konwertery WPF (value converters)
├── App.xaml
└── db_script.sql                       ← pomocniczy skrypt SQL (nieautorytatywny)
```

## Zmiana wersji aplikacji

⚠️ **ZAWSZE przy zmianie wersji aktualizuj OBA miejsca jednocześnie:**

1. `InwentaryzacjaSprzetu.csproj` — właściwości `<Version>`, `<AssemblyVersion>`, `<FileVersion>`
2. `Properties/AssemblyInfo.cs` — atrybuty `[AssemblyVersion]`, `[AssemblyFileVersion]`, `[AssemblyInformationalVersion]`

**Dlaczego:** Projekt ma `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>`, więc atrybuty z `AssemblyInfo.cs` mają pierwszeństwo nad `.csproj`. WPF generuje pack URI z wersją z `.csproj` (np. `/InwentaryzacjaSprzetu;V1.5.3.0;component/app.xaml`), ale assembly w runtime ma wersję z `AssemblyInfo.cs` — niezgodność powoduje `FileNotFoundException` przy starcie aplikacji.

Po każdej zmianie wersji wymagane jest `dotnet clean` + `dotnet build` (pełna rekompilacja).

## Komendy projektu
```bash
# Budowanie projektu
dotnet build

# Uruchomienie (bezpośrednio przez .exe — dotnet run nie działa poprawnie z WPF pack URI)
.\bin\Debug\net10.0-windows\InwentaryzacjaSprzetu.exe

# Testy (gdy dodane)
dotnet test
```
