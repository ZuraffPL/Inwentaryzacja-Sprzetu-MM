# Szybkie uruchomienie - PowerShell
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "     Inwentaryzacja Sprzętu - Uruchomienie" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Sprawdź czy .NET SDK jest zainstalowany
Write-Host "Sprawdzanie .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version 2>$null
    Write-Host "✅ .NET SDK jest zainstalowany: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ BŁĄD: .NET 8.0 SDK nie jest zainstalowany!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Pobierz i zainstaluj .NET 8.0 SDK z:" -ForegroundColor Yellow
    Write-Host "https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Blue
    Write-Host ""
    Write-Host "Wybierz '.NET 8.0 SDK' (nie Runtime)" -ForegroundColor Yellow
    Read-Host "Naciśnij Enter aby zakończyć"
    exit 1
}

# Przejdź do folderu projektu
$projectPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectPath

Write-Host ""
Write-Host "Przywracanie pakietów NuGet..." -ForegroundColor Yellow
$restoreResult = dotnet restore InwentaryzacjaSprzetu.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Błąd podczas przywracania pakietów" -ForegroundColor Red
    Read-Host "Naciśnij Enter aby zakończyć"
    exit 1
}
Write-Host "✅ Pakiety przywrócone" -ForegroundColor Green

Write-Host ""
Write-Host "Kompilowanie aplikacji..." -ForegroundColor Yellow
$buildResult = dotnet build InwentaryzacjaSprzetu.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Błąd kompilacji" -ForegroundColor Red
    Read-Host "Naciśnij Enter aby zakończyć"
    exit 1
}
Write-Host "✅ Kompilacja zakończona sukcesem" -ForegroundColor Green

Write-Host ""
Write-Host "Uruchamianie aplikacji..." -ForegroundColor Yellow
Write-Host "===============================================" -ForegroundColor Cyan
.\bin\Debug\net10.0-windows7.0\InwentaryzacjaSprzetu.exe

Write-Host ""
Write-Host "Aplikacja została zamknięta." -ForegroundColor Gray
Read-Host "Naciśnij Enter aby zakończyć"