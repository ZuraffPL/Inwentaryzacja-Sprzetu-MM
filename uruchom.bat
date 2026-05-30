@echo off
chcp 65001 >nul
cd /d "%~dp0"

echo Uruchamianie Inwentaryzacja Sprzetu...
echo (kompilacja przyrostowa - tylko zmienione pliki)
echo.

dotnet build InwentaryzacjaSprzetu.csproj
if errorlevel 1 (
    echo.
    echo BLAD: Kompilacja nieudana.
    pause
    exit /b 1
)

.\bin\Debug\net10.0-windows7.0\InwentaryzacjaSprzetu.exe

if errorlevel 1 (
    echo.
    echo BLAD: Aplikacja nie uruchomila sie poprawnie.
    pause
)