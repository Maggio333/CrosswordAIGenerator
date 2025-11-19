namespace CrosswordAIGenerator.Core.Domain.Services;

/// <summary>
/// Logger specjalnie dla Cursora (AI) - do debugowania i śledzenia działania aplikacji
/// </summary>
public interface ICursorLogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
    
    void DebugFormat(string format, params object[] args);
    void InfoFormat(string format, params object[] args);
    void WarningFormat(string format, params object[] args);
    void ErrorFormat(string format, Exception? exception, params object[] args);
}

