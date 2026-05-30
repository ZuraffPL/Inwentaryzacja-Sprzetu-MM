@echo off
chcp 65001 >nul
cd /d "%~dp0"

echo ================================================================
echo   CZYSZCZENIE: bin, obj, publish, stare ZIP/EXE
setlocal enableextensions enabledelayedexpansion

REM --- Usuwanie katalogów bin i obj ---
for %%D in (bin obj) do (
    if exist "%%D" (
        echo Usuwam katalog %%D ...
        rmdir /s /q "%%D"
    )
)

REM --- Usuwanie wszystkiego z publish ---
if exist "publish" (
    echo Usuwam wszystko z publish ...
    rmdir /s /q "publish"
)

REM --- Usuwanie starych ZIP i EXE (zostaw najnowszy) ---
set "LASTZIP="
set "LASTEXE="
for /f "delims=" %%F in ('dir /b /a:-d /o-d InwentaryzacjaSprzetu_*.zip 2^>nul') do (
    if not defined LASTZIP (
        set "LASTZIP=%%F"
    ) else (
        echo Usuwam stary ZIP: %%F
        del /q "%%F"
    )
)
for /f "delims=" %%F in ('dir /b /a:-d /o-d InwentaryzacjaSprzetu_Setup_*.exe 2^>nul') do (
    if not defined LASTEXE (
        set "LASTEXE=%%F"
    ) else (
        echo Usuwam stary EXE: %%F
        del /q "%%F"
    )
)

echo.
echo [OK] Skończono czyszczenie. Najnowszy ZIP i EXE zostawiony.
echo Nacisnij dowolny klawisz, aby zamknac...
pause >nul
