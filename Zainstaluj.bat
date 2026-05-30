@echo off
chcp 65001 >nul 2>&1

:: ================================================================
:: Zainstaluj.bat — Instalator Inwentaryzacja Sprzętu
:: Dwuklik wystarczy — sam poprosi o uprawnienia administratora
:: ================================================================

:: Sprawdź czy już mamy uprawnienia admina
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Potrzebne uprawnienia administratora — za chwilę pojawi się okno UAC...
    timeout /t 2 /nobreak >nul
    powershell -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

:: Uruchom skrypt instalacyjny (ExecutionPolicy Bypass — nie zmienia ustawień systemowych)
powershell -ExecutionPolicy Bypass -NoProfile -File "%~dp0Setup-Install.ps1"

exit /b
