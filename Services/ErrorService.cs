using System;
using System.Collections.ObjectModel;
using System.IO;
using ATLab.Interfaces;

namespace ATLab.Services;

public class ErrorService : IErrorService
{
    private readonly string _logFilePath;
    private readonly object _lock = new();

    public ObservableCollection<string> Errors { get; } = new();

    public ErrorService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ATLab"
        );

        Directory.CreateDirectory(dir);
        _logFilePath = Path.Combine(dir, "errors.log");
    }

    public void AddError(string message)
    {
        var timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        string formattedMessage = $"[{timestamp}] {message}";

        Errors.Insert(0, formattedMessage);

        lock (_lock)
        {
            try
            {
                File.AppendAllLines(_logFilePath, [formattedMessage]);
            }
            catch (Exception ex)
            {
                Errors.Insert(0, $"[{timestamp}] Critical: Could not write to log file: {ex.Message}");
            }
        }

        ErrorsChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? ErrorsChanged;
}