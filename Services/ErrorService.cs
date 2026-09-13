using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace ATLab.Services;

public partial class ErrorService : ObservableObject, IErrorService
{
    private readonly string _logFilePath;
    private readonly object _lock = new();

    public ObservableCollection<string> Errors { get; } = new();

    [ObservableProperty]
    private ToastManager _toastManager;

    public ErrorService(ToastManager toastManager)
    {
        ToastManager = toastManager;
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ATLab"
        );

        Directory.CreateDirectory(dir);
        _logFilePath = Path.Combine(dir, "errors.log");
    }

    public void AddError(
        string message,
        [CallerFilePath] string file = "",
        [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0)
    {
        var timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

        var formattedMessage =
            $"[{timestamp}] {message} (at {Path.GetFileName(file)}:{line} in {member}())";
        
        ToastManager.CreateToast("Error")
            .WithContent(message)
            .OnBottomLeft()
            .WithDelay(5)
            .ShowError();

        Errors.Insert(0, formattedMessage);

        lock (_lock)
        {
            try
            {
                File.AppendAllLines(_logFilePath, new[] { formattedMessage });
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