@echo off
echo ===============================================
echo       Zarządzanie Migracjami Bazy Danych
echo ===============================================
echo.

if "%1"=="" goto menu
if "%1"=="add" goto add_migration
if "%1"=="update" goto update_database
if "%1"=="remove" goto remove_migration
goto menu

:menu
echo Dostępne komendy:
echo   migracje.bat add [nazwa]     - Dodaj nową migrację
echo   migracje.bat update          - Zaktualizuj bazę danych
echo   migracje.bat remove          - Usuń ostatnią migrację
echo.
echo Aktualne migracje:
dotnet ef migrations list
echo.
pause
goto end

:add_migration
if "%2"=="" (
    echo Błąd: Musisz podać nazwę migracji
    echo Przykład: migracje.bat add AddNewField
    goto end
)
echo Tworzenie nowej migracji: %2
dotnet ef migrations add %2
if errorlevel 1 (
    echo ❌ Błąd podczas tworzenia migracji
    pause
    goto end
)
echo ✅ Migracja %2 została utworzona
goto end

:update_database
echo Aktualizowanie bazy danych...
dotnet ef database update
if errorlevel 1 (
    echo ❌ Błąd podczas aktualizacji bazy danych
    pause
    goto end
)
echo ✅ Baza danych została zaktualizowana
goto end

:remove_migration
echo Usuwanie ostatniej migracji...
dotnet ef migrations remove
if errorlevel 1 (
    echo ❌ Błąd podczas usuwania migracji
    pause
    goto end
)
echo ✅ Ostatnia migracja została usunięta
goto end

:end