using System;
using System.IO;

namespace Void.Services;

public class LoggingService
{
    private readonly string _logPath;
    
    public LoggingService()
    {
        _logPath = "logs";
        Directory.CreateDirectory(_logPath);
    }
    
    public void Info(string message)
    {
        Log("INFO", message);
    }
    
    public void Warning(string message)
    {
        Log("WARN", message);
    }
    
    public void Error(string message)
    {
        Log("ERROR", message);
    }
    
    public void Error(Exception ex, string message)
    {
        Log("ERROR", $"{message} - {ex.Message}");
    }
    
    private void Log(string level, string message)
    {
        var logFile = Path.Combine(_logPath, $"void-{DateTime.Now:yyyy-MM-dd}.log");
        var logLine = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
        
        File.AppendAllText(logFile, logLine);
        Console.WriteLine(logLine.Trim());
    }
}