; =============================================================================
; installer.iss — Inno Setup 6 — Skrypt instalacyjny
; Inwentaryzacja Sprzętu
; UWAGA: Wersja jest przekazywana przez Publish-App.ps1 (/DAppVersion=X.Y.Z).
;        Poniższy #define to fallback — aktualna wersja pochodzi z .csproj.
; =============================================================================
; Wymagania (jednorazowe na komputerze deweloperskim):
;   1. Inno Setup 6  ->  https://jrsoftware.org/isdl.php
;   2. Folder "publish\" wypełniony przez: .\Publish-App.ps1
;
; Użycie:
;   Automatycznie: .\Publish-App.ps1  (kompiluje dokładnie w odpowiedniej kolejności)
;   Ręcznie:       "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
; =============================================================================

#define AppName      "Inwentaryzacja Sprzętu"
#ifndef AppVersion
  #define AppVersion "1.5.3"
#endif
#define AppPublisher "Merkury Market"
#define AppExeName   "InwentaryzacjaSprzetu.exe"

[Setup]
; AppId — stały identyfikator GUID. NIE zmieniaj między wersjami (potrzebny do deinstalacji/aktualizacji).
AppId={{3D8F2A71-E4B6-4C9A-B8D3-5F1E6A2C7B4D}}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\InwentaryzacjaSprzetu
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=InwentaryzacjaSprzetu_Setup_{#AppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
; Aplikacja jest skompilowana jako win-x64 — wymagany system 64-bit
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
WizardStyle=modern
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName} {#AppVersion}
PrivilegesRequired=admin
MinVersion=10.0

[Languages]
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"

[Tasks]
Name: "desktopicon"; Description: "Utwórz skrót na pulpicie"

[Files]
; Self-contained aplikacja — runtime .NET 9 jest dołączony (NIE wymaga instalacji .NET na docelowym PC)
; WAŻNE (dla dewelopera): przed kompilacją uruchom .\Publish-App.ps1,
;        która buduje świeże pliki do folderu publish\.
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}";       Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Uruchom {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
// Przy pierwszej instalacji kopiuje szablon bazy danych do %LOCALAPPDATA%\InwentaryzacjaSprzetu\.
// Przy aktualizacji istniejącej instalacji baza danych NIE jest nadpisywana.
procedure CurStepChanged(CurStep: TSetupStep);
var
  DbDir, DbPath, TemplatePath: string;
begin
  if CurStep = ssPostInstall then
  begin
    DbDir        := ExpandConstant('{localappdata}\InwentaryzacjaSprzetu');
    DbPath       := DbDir + '\inventory.db';
    TemplatePath := ExpandConstant('{app}\inventory.db.template');

    if not DirExists(DbDir) then
      ForceDirectories(DbDir);

    // Świeża instalacja — skopiuj szablon bazy (tylko gdy nie istnieje własna baza)
    if (not FileExists(DbPath)) and FileExists(TemplatePath) then
      FileCopy(TemplatePath, DbPath, False);
  end;
end;
