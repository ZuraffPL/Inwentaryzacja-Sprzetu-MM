# =============================================================================
# Publish-App.ps1 — Budowanie paczki instalacyjnej Inwentaryzacja Sprzętu
# =============================================================================
# Wymagania:
#   - .NET 9 SDK zainstalowany
#   - Uruchom z uprawnieniami normalnego użytkownika (nie admin)
#
# Użycie:
#   .\Publish-App.ps1
#   .\Publish-App.ps1 -OutputDir "D:\Deploy\v1.4.0"
# =============================================================================

param(
    [string]$OutputDir = ".\publish"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectFile = Join-Path $ScriptDir "InwentaryzacjaSprzetu.csproj"

Write-Host "\n=== Inwentaryzacja Sprzetu - Budowanie paczki instalacyjnej ===\n" -ForegroundColor Cyan

# --- Sprawdź czy .NET SDK jest dostępny ---
try {
    $dotnetVersion = & dotnet --version 2>&1
    Write-Host "[OK] .NET SDK: $dotnetVersion" -ForegroundColor Green
}
catch {
    Write-Host "[BLAD] Nie znaleziono .NET SDK. Zainstaluj .NET 9 SDK z https://dot.net" -ForegroundColor Red
    exit 1
}

# --- Wyczyść poprzednią paczkę ---
$ResolvedOutput = Join-Path $ScriptDir $OutputDir
if (Test-Path $ResolvedOutput) {
    Write-Host "Czyszczenie poprzedniej wersji w: $ResolvedOutput"
    Remove-Item -Recurse -Force $ResolvedOutput
}

# --- Publish: self-contained, win-x64, single-directory ---
Write-Host "\nBudowanie (Release / win-x64 / self-contained)..."
& dotnet publish $ProjectFile `
    --runtime win-x64 `
    --self-contained true `
    --configuration Release `
    --output $ResolvedOutput `
    /p:PublishSingleFile=false `
    /p:PublishReadyToRun=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "\n[BLAD] Blad podczas budowania! Sprawdz powyzsze komunikaty." -ForegroundColor Red
    exit 1
}

Write-Host "\n[OK] Budowanie zakonczone pomyslnie." -ForegroundColor Green

# --- Skopiuj skrypty instalacyjne do paczki ---
$SetupScript = Join-Path $ScriptDir "Setup-Install.ps1"
if (Test-Path $SetupScript) {
    Copy-Item $SetupScript (Join-Path $ResolvedOutput "Setup-Install.ps1")
    Write-Host "[OK] Skopiowano Setup-Install.ps1 do paczki." -ForegroundColor Green
}
$InstallerBat = Join-Path $ScriptDir "Zainstaluj.bat"
if (Test-Path $InstallerBat) {
    Copy-Item $InstallerBat (Join-Path $ResolvedOutput "Zainstaluj.bat")
    Write-Host "[OK] Skopiowano Zainstaluj.bat do paczki." -ForegroundColor Green
}

# --- Skopiuj aktualną bazę danych jako szablon dla świeżej instalacji ---
$DbSource = Join-Path $env:LOCALAPPDATA "InwentaryzacjaSprzetu\inventory.db"
if (-not (Test-Path $DbSource)) {
    # Fallback na starą lokalizację (katalog projektu)
    $DbSource = Join-Path $ScriptDir "inventory.db"
}
if (Test-Path $DbSource) {
    Copy-Item $DbSource (Join-Path $ResolvedOutput "inventory.db.template")
    $DbSize = [math]::Round((Get-Item $DbSource).Length / 1KB, 0)
    Write-Host "[OK] Skopiowano baze danych jako szablon ($DbSize KB -> inventory.db.template)." -ForegroundColor Green
} else {
    Write-Host "[UWAGA] Nie znaleziono pliku bazy danych - swieza instalacja bedzie startowac z pusta baza." -ForegroundColor Yellow
}

# --- Stwórz ZIP do dystrybucji ---
$ZipName = "InwentaryzacjaSprzetu_$(Get-Date -Format 'yyyyMMdd_HHmm').zip"
$ZipPath = Join-Path $ScriptDir $ZipName

Write-Host "\nTworzenie archiwum ZIP: $ZipName"
Compress-Archive -Path "$ResolvedOutput\*" -DestinationPath $ZipPath -Force

if (Test-Path $ZipPath) {
    $ZipSize = [math]::Round((Get-Item $ZipPath).Length / 1MB, 1)
    Write-Host "[OK] Archiwum: $ZipPath ($ZipSize MB)" -ForegroundColor Green
}

# --- Kompilacja instalatora .exe (Inno Setup 6) ---
$IssFile      = Join-Path $ScriptDir "installer.iss"
$InstallerExe = $null
$IsccPaths    = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)
$IsccExe = $IsccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($IsccExe -and (Test-Path $IssFile)) {
    # Pobierz aktualną wersję z .csproj
    [xml]$csprojXml = Get-Content $ProjectFile
    $appVersion = ($csprojXml.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ }) | Select-Object -First 1
    if (-not $appVersion) { $appVersion = "1.0.0" }
    Write-Host "\nWersja aplikacji: $appVersion" -ForegroundColor Cyan
    Write-Host "Kompilacja instalatora .exe (Inno Setup 6)..."
    & $IsccExe "/DAppVersion=$appVersion" $IssFile
    if ($LASTEXITCODE -eq 0) {
        $InstallerExe = Get-ChildItem $ScriptDir -Filter "InwentaryzacjaSprzetu_Setup_*.exe" |
                        Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($InstallerExe) {
            $ExeSize = [math]::Round($InstallerExe.Length / 1MB, 1)
            Write-Host "[OK] Installer: $($InstallerExe.FullName) ($ExeSize MB)" -ForegroundColor Green
        }
    } else {
        Write-Host "[BLAD] Blad kompilacji instalatora!" -ForegroundColor Red
    }
} elseif (-not $IsccExe) {
    Write-Host "\n[UWAGA] Inno Setup 6 nie jest zainstalowany - pomijanie generowania .exe" -ForegroundColor Yellow
    Write-Host "  Pobierz i zainstaluj (jednorazowo): https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host "  Potem uruchom Publish-App.ps1 ponownie." -ForegroundColor Yellow
}

$InstallerLine = if ($InstallerExe) {
    "  Installer: $($InstallerExe.FullName)"
} else {
    "  Installer: (uruchom ponownie po zainstalowaniu Inno Setup 6)"
}

Write-Host @"

=== Gotowe! ===

Paczka instalacyjna:
  Katalog: $ResolvedOutput
  ZIP:     $ZipPath
$InstallerLine

Instalacja na komputerze docelowym:
  OPCJA 1 (zalecana):
    Skopiuj plik InwentaryzacjaSprzetu_Setup_*.exe na pendrive.
    Na docelowym PC: dwuklik na .exe → kreator instalacji → Dalej, Dalej, Zakończ.
    (NIE wymaga osobnej instalacji .NET — runtime jest dołączony)

  OPCJA 2 (alternatywna):
    Skopiuj ZIP na pendrive, rozpakuj, dwuklik Zainstaluj.bat.
    (Pojawi się okno UAC — kliknij Tak)

Baza danych:
  %LOCALAPPDATA%\InwentaryzacjaSprzetu\inventory.db
  (zachowywana przy aktualizacjach — nie jest nadpisywana)

"@ -ForegroundColor Yellow
