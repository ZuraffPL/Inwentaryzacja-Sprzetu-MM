# =============================================================================
# Setup-Install.ps1 — Instalacja Inwentaryzacja Sprzętu na komputerze docelowym
# =============================================================================
# Uruchom jako ADMINISTRATOR z katalogu, w którym są pliki aplikacji:
#
#   .\Setup-Install.ps1
#   .\Setup-Install.ps1 -AppDir "C:\InwentaryzacjaSprzetu" -MigrateDb "D:\stara_baza\inventory.db"
#
# Parametry:
#   -AppDir     Katalog instalacji (domyślnie: C:\InwentaryzacjaSprzetu)
#   -MigrateDb  Ścieżka do istniejącej bazy danych do przeniesienia (opcjonalne)
# =============================================================================

param(
    [string]$AppDir    = "C:\InwentaryzacjaSprzetu",
    [string]$MigrateDb = ""
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Katalog bazy danych (AppData — chroniony przed nadpisaniem przy aktualizacji)
$DbDir     = Join-Path $env:LOCALAPPDATA "InwentaryzacjaSprzetu"
$DbPath    = Join-Path $DbDir "inventory.db"
$ExeName   = "InwentaryzacjaSprzetu.exe"
$ShortcutName = "Inwentaryzacja Sprzętu.lnk"

Write-Host "`n=== Instalacja Inwentaryzacja Sprzętu ===`n" -ForegroundColor Cyan

# --- Sprawdź uprawnienia administratora ---
$IsAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $IsAdmin) {
    Write-Host "✗ Skrypt wymaga uprawnień administratora." -ForegroundColor Red
    Write-Host "  Kliknij prawym przyciskiem na PowerShell → 'Uruchom jako administrator'" -ForegroundColor Yellow
    pause
    exit 1
}

# --- Utwórz katalog instalacji ---
Write-Host "Instalowanie aplikacji do: $AppDir"
if (-not (Test-Path $AppDir)) {
    New-Item -ItemType Directory -Path $AppDir -Force | Out-Null
}

# --- Skopiuj pliki aplikacji ---
Write-Host "Kopiowanie plików aplikacji..."
$FilesToCopy = Get-ChildItem -Path $ScriptDir -Exclude "Setup-Install.ps1", "*.zip"
foreach ($File in $FilesToCopy) {
    $Dest = Join-Path $AppDir $File.Name
    if ($File.PSIsContainer) {
        Copy-Item -Recurse -Force $File.FullName $Dest
    } else {
        Copy-Item -Force $File.FullName $Dest
    }
}
Write-Host "✓ Pliki skopiowane." -ForegroundColor Green

# --- Utwórz katalog bazy danych ---
if (-not (Test-Path $DbDir)) {
    New-Item -ItemType Directory -Path $DbDir -Force | Out-Null
    Write-Host "✓ Utworzono katalog bazy: $DbDir" -ForegroundColor Green
}

# --- Przenieś istniejącą bazę danych (jeśli podano przez parametr) ---
if ($MigrateDb -ne "" -and (Test-Path $MigrateDb)) {
    Write-Host "Przenoszenie istniejącej bazy danych z: $MigrateDb"
    if (Test-Path $DbPath) {
        $BackupPath = $DbPath + ".backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
        Copy-Item $DbPath $BackupPath
        Write-Host "  Kopia zapasowa zapisana: $BackupPath" -ForegroundColor Gray
    }
    Copy-Item $MigrateDb $DbPath -Force
    Write-Host "✓ Baza danych przeniesiona z podanej ściezki." -ForegroundColor Green
} elseif ($MigrateDb -ne "") {
    Write-Host "⚠ Podana ścieżka bazy nie istnieje: $MigrateDb" -ForegroundColor Yellow
    Write-Host "  Sprawdzona zostanie baza domyślna z paczki." -ForegroundColor Yellow
}

# --- Świeża instalacja: skopiuj bazę szablonową z paczki (tylko gdy brak istniejącej) ---
if (-not (Test-Path $DbPath)) {
    $TemplateDb = Join-Path $ScriptDir "inventory.db.template"
    if (Test-Path $TemplateDb) {
        Copy-Item $TemplateDb $DbPath -Force
        Write-Host "✓ Zainstalowano bazę danych z szablonu paczki." -ForegroundColor Green
        Write-Host "  Lokalizacja: $DbPath"
    } else {
        Write-Host "ℹ Brak szablonu bazy — aplikacja utworzy nową bazę przy pierwszym uruchomieniu." -ForegroundColor Cyan
        Write-Host "  Lokalizacja: $DbPath"
    }
} else {
    Write-Host "✓ Istniejąca baza danych zachowana (aktualizacja)." -ForegroundColor Green
    Write-Host "  Lokalizacja: $DbPath"
}

# --- Utwórz skrót na Pulpicie ---
Write-Host "`nTworzenie skrótu na Pulpicie..."
try {
    $DesktopPath  = [Environment]::GetFolderPath("CommonDesktopDirectory")
    $ShortcutPath = Join-Path $DesktopPath $ShortcutName
    $ExePath      = Join-Path $AppDir $ExeName

    $WshShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath       = $ExePath
    $Shortcut.WorkingDirectory = $AppDir
    $Shortcut.Description      = "Inwentaryzacja Sprzętu — system zarządzania sprzętem"
    $Shortcut.Save()

    Write-Host "✓ Skrót utworzony: $ShortcutPath" -ForegroundColor Green
} catch {
    Write-Host "⚠ Nie udało się utworzyć skrótu: $_" -ForegroundColor Yellow
    Write-Host "  Uruchom aplikację ręcznie z: $(Join-Path $AppDir $ExeName)"
}

Write-Host @"

=== Instalacja zakończona! ===

Aplikacja zainstalowana w:  $AppDir
Baza danych przechowywana: $DbPath
Skrót na Pulpicie:         $ShortcutName

Uruchom aplikację przez skrót na Pulpicie lub:
  $(Join-Path $AppDir $ExeName)

"@ -ForegroundColor Green

pause
