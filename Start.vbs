' Start.vbs — uruchamia aplikację bez okna konsoli
' Dwuklik → aplikacja otwiera się bezpośrednio, bez żadnych okien terminala

Set oShell = CreateObject("WScript.Shell")
Set oFSO   = CreateObject("Scripting.FileSystemObject")

sDir = oFSO.GetParentFolderName(WScript.ScriptFullName)

' windowStyle=0 → ukryta konsola; bWaitOnReturn=True → czekaj na zamknięcie aplikacji
Dim exitCode
exitCode = oShell.Run("cmd /c cd /d """ & sDir & """ && dotnet run --project InwentaryzacjaSprzetu.csproj", 0, True)

If exitCode <> 0 Then
    MsgBox "Błąd podczas uruchamiania aplikacji (kod: " & exitCode & ")." & vbCrLf & vbCrLf & _
           "Sprawdź czy projekt się kompiluje, uruchamiając uruchom.bat.", _
           vbCritical, "Inwentaryzacja Sprzętu — błąd"
End If
