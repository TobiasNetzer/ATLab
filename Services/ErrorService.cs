using System;
using System.Collections.ObjectModel;
using ATLab.Interfaces;

namespace ATLab.Services;

public class ErrorService : IErrorService
{
    public ObservableCollection<string> Errors { get; } = new();

    public void AddError(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Errors.Insert(0, $"[{timestamp}] {message}");
        ErrorsChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? ErrorsChanged;
}