using System.Diagnostics;
using System.IO;
using CrosswordAIGenerator.Core.Domain.Services;

namespace CrosswordAIGenerator.Core.Infrastructure.Services;

/// <summary>
/// Logger specjalnie dla Cursora (AI) - loguje do pliku i Debug output
/// </summary>
public class CursorLogger : ICursorLogger
{
    private readonly string _logFilePath;
    private readonly object _lockObject = new object();
    private const string CURSOR_TAG = "[CURSOR]";

    public CursorLogger()
    {
        // Utwórz katalog logs jeśli nie istnieje
        var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        if (!Directory.Exists(logsDir))
        {
            Directory.CreateDirectory(logsDir);
        }

        // Plik logów z datą
        var dateStr = DateTime.Now.ToString("yyyy-MM-dd");
        _logFilePath = Path.Combine(logsDir, $"cursor_{dateStr}.log");
    }

    public void Debug(string message)
    {
        Log("DEBUG", message);
    }

    public void Info(string message)
    {
        Log("INFO", message);
    }

    public void Warning(string message)
    {
        Log("WARNING", message);
    }

    public void Error(string message, Exception? exception = null)
    {
        var fullMessage = message;
        if (exception != null)
        {
            fullMessage += $"\nException: {exception.GetType().Name}: {exception.Message}\nStack Trace: {exception.StackTrace}";
        }
        Log("ERROR", fullMessage);
    }

    public void DebugFormat(string format, params object[] args)
    {
        Debug(string.Format(format, args));
    }

    public void InfoFormat(string format, params object[] args)
    {
        Info(string.Format(format, args));
    }

    public void WarningFormat(string format, params object[] args)
    {
        Warning(string.Format(format, args));
    }

    public void ErrorFormat(string format, Exception? exception, params object[] args)
    {
        Error(string.Format(format, args), exception);
    }

    private void Log(string level, string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"{CURSOR_TAG} [{timestamp}] [{level}] {message}";

        // Log do Debug output (widoczne w Visual Studio Output window)
        System.Diagnostics.Debug.WriteLine(logEntry);

        // Log do pliku
        lock (_lockObject)
        {
            try
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Jeśli nie można zapisać do pliku, tylko Debug output
                System.Diagnostics.Debug.WriteLine($"{CURSOR_TAG} [ERROR] Nie można zapisać do pliku logów: {ex.Message}");
            }
        }
    }
}

