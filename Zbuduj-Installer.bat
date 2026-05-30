@echo off
chcp 65001 >nul
cd /d "%~dp0"

echo.
echo ================================================================
echo   Inwentaryzacja Sprzetu -- Budowanie instalatora
echo   (self-contained: .NET runtime dolaczony, brak zaleznosci)
echo ================================================================
echo.
echo Krok 1/3 : dotnet publish  (Release / win-x64 / self-contained)
echo Krok 2/3 : kopiowanie szablonu bazy danych
echo Krok 3/3 : kompilacja instalatora .exe  (Inno Setup 6)
echo.
echo Nacisnij dowolny klawisz, aby kontynuowac... lub zamknij okno.
pause >nul

echo.
echo --- Start: %DATE% %TIME% ---
echo.

powershell.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0Publish-App.ps1"
set BUILD_RESULT=%ERRORLEVEL%

echo.
if %BUILD_RESULT% EQU 0 (
    echo ================================================================
    echo   SUKCES  --  instalator jest gotowy w katalogu projektu.
    echo   Plik: InwentaryzacjaSprzetu_Setup_*.exe
    echo ================================================================
) else (
    echo ================================================================
    echo   BLAD  --  sprawdz komunikaty powyzej.
    echo   Kod bledu: %BUILD_RESULT%
    echo ================================================================
)

echo.
echo Nacisnij dowolny klawisz, aby zamknac...
pause >nul
