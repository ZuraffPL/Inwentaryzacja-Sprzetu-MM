# Instrukcja instalacji — Inwentaryzacja Sprzętu

## Wymagania

- Windows 10 lub nowszy (x64)
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Sprawdzenie instalacji .NET

```powershell
dotnet --version
# Powinno pokazać 9.0.x
```

Jeśli komenda nie jest rozpoznana — pobierz i zainstaluj SDK, następnie zrestartuj terminal.

## Uruchomienie

### Metoda 1 — skrypt PowerShell (zalecana)

```powershell
.\uruchom.ps1
```

Skrypt automatycznie buduje projekt i uruchamia aplikację.

### Metoda 2 — ręcznie w konsoli

```powershell
cd "d:\Inwentaryzacja Sprzętu"
dotnet restore
dotnet build
dotnet run
```

### Metoda 3 — VS Code

1. Otwórz folder projektu: `File → Open Folder`
2. Zainstaluj rozszerzenie **C# Dev Kit**
3. Uruchom: `Terminal → Run Build Task` → potem `dotnet run`

## Pierwsze uruchomienie

Przy pierwszym starcie aplikacja automatycznie:
1. Tworzy bazę SQLite `inventory.db` w katalogu projektu
2. Uruchamia migracje EF Core (tworzy schemat tabel)
3. Ładuje dane startowe (domyślne kategorie i lokalizacje)

Pojawi się ekran powitalny — aplikacja jest gotowa do pracy.

## Rozwiązywanie problemów

### `dotnet: The term 'dotnet' is not recognized`

Nie zainstalowany .NET 9 SDK. Pobierz z <https://dotnet.microsoft.com/download/dotnet/9.0>, zainstaluj i zrestartuj terminal.

### Błędy przy przywracaniu pakietów NuGet

```powershell
dotnet clean
dotnet restore
dotnet build
```

### Brak uprawnień zapisu (błąd bazy danych)

Upewnij się, że folder projektu (`d:\Inwentaryzacja Sprzętu`) nie jest tylko do odczytu. Ewentualnie uruchom terminal jako administrator.

### Błąd kompilacji po aktualizacji pliku projektu

```powershell
dotnet clean
dotnet build "InwentaryzacjaSprzetu.csproj"
```

## Migracje bazy danych

Po jakiejkolwiek zmianie w modelach (`Models/`), należy dodać migrację i przebudować:

```powershell
dotnet ef migrations add NazwaMigracji
dotnet build
```

Migracje są stosowane automatycznie przy starcie aplikacji (`MigrateAsync()`).

## Backup bazy danych

```powershell
Copy-Item "d:\Inwentaryzacja Sprzętu\inventory.db" `
          "d:\backup\inventory_$(Get-Date -Format 'yyyyMMdd_HHmm').db"
```
