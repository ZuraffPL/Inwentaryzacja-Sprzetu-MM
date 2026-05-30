using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InwentaryzacjaSprzetu.Services
{
    public interface ILoggingService
    {
        Task LogErrorAsync(string message, Exception? exception = null);
        Task LogInfoAsync(string message);
        Task LogWarningAsync(string message);
    }

    public class LoggingService : ILoggingService
    {
        private readonly string _logFilePath;
        private static readonly object _lock = new object();
        private static readonly string Separator = new string('-', 80);

        private const long MaxFileSizeBytes = 5L * 1024 * 1024;  // 5 MB
        private const long MinBytesToFree   = 512L * 1024;        // 0,5 MB

        public LoggingService()
        {
            _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_log.txt");
        }

        public async Task LogErrorAsync(string message, Exception? exception = null)
            => await WriteLogAsync("ERROR", message, exception);

        public async Task LogInfoAsync(string message)
            => await WriteLogAsync("INFO", message);

        public async Task LogWarningAsync(string message)
            => await WriteLogAsync("WARNING", message);

        private async Task WriteLogAsync(string level, string message, Exception? exception = null)
        {
            try
            {
                var entry = FormatEntry(level, message, exception);
                lock (_lock)
                {
                    EnsureSizeLimit();
                    Prepend(entry);
                }
                System.Console.Write(entry);
            }
            catch
            {
                // Nie rób nic — aby uniknąć nieskończonej pętli błędów
            }
            await Task.CompletedTask;
        }

        private void EnsureSizeLimit()
        {
            if (!File.Exists(_logFilePath))
                return;
            if (new FileInfo(_logFilePath).Length > MaxFileSizeBytes)
                TrimOldest();
        }

        /// <summary>
        /// Usuwa najstarsze wpisy (koniec pliku) dopóki nie zostanie zwolnione ≥ MinBytesToFree.
        /// Nowe wpisy są dopisywane na początku, więc koniec pliku = najstarsze.
        /// </summary>
        private void TrimOldest()
        {
            try
            {
                var allLines = File.ReadAllLines(_logFilePath, Encoding.UTF8);

                // Zgrupuj linie w bloki wpisów — każdy blok kończy się wierszem separatora
                var entries = new List<List<string>>();
                var current = new List<string>();
                foreach (var line in allLines)
                {
                    current.Add(line);
                    if (line == Separator)
                    {
                        entries.Add(current);
                        current = new List<string>();
                    }
                }
                if (current.Count > 0)
                    entries.Add(current);

                // Usuwaj od końca (najstarsze) dopóki nie zwolnimy >= MinBytesToFree
                long freed = 0;
                while (entries.Count > 1 && freed < MinBytesToFree)
                {
                    var last = entries[^1];
                    freed += Encoding.UTF8.GetByteCount(
                        string.Join(Environment.NewLine, last) + Environment.NewLine);
                    entries.RemoveAt(entries.Count - 1);
                }

                File.WriteAllLines(_logFilePath, entries.SelectMany(e => e), Encoding.UTF8);
            }
            catch { }
        }

        private void Prepend(string entry)
        {
            try
            {
                var existing = File.Exists(_logFilePath)
                    ? File.ReadAllText(_logFilePath, Encoding.UTF8)
                    : string.Empty;
                File.WriteAllText(_logFilePath, entry + existing, Encoding.UTF8);
            }
            catch
            {
                // Fallback — jeśli nie można zapisać na początku, dopisz na końcu
                File.AppendAllText(_logFilePath, entry, Encoding.UTF8);
            }
        }

        private string FormatEntry(string level, string message, Exception? exception = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");

            if (exception != null)
            {
                sb.AppendLine($"Exception: {exception.GetType().Name}: {exception.Message}");
                if (!string.IsNullOrEmpty(exception.StackTrace))
                    sb.AppendLine($"StackTrace: {exception.StackTrace}");
                if (exception.InnerException != null)
                    sb.AppendLine($"InnerException: {exception.InnerException.GetType().Name}: {exception.InnerException.Message}");
            }

            sb.AppendLine(Separator);
            return sb.ToString();
        }
    }
}