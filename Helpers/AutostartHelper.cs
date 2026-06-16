using System;
using System.Reflection;
using Microsoft.Win32;

namespace InwentaryzacjaSprzetu.Helpers
{
    /// <summary>
    /// Zarządza wpisem autostartu aplikacji w rejestrze Windows
    /// (HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run).
    /// Nie wymaga uprawnień administratora.
    /// </summary>
    internal static class AutostartHelper
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName    = "InwentaryzacjaSprzetu";

        /// <summary>Zwraca true jeśli wpis autostartu istnieje w rejestrze.</summary>
        public static bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
                return key?.GetValue(AppName) is not null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Włącza lub wyłącza autostart aplikacji.</summary>
        public static void SetEnabled(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
                if (key == null) return;

                if (enable)
                {
                    // Ścieżka do aktualnie uruchomionego pliku exe
                    var exePath = Environment.ProcessPath
                        ?? Assembly.GetExecutingAssembly().Location;
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(AppName, throwOnMissingValue: false);
                }
            }
            catch { /* błąd rejestru — niekrytyczny */ }
        }
    }
}
