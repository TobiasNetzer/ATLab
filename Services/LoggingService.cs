using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Records;
using CommunityToolkit.Mvvm.ComponentModel;
using ShadUI;

namespace ATLab.Services;

public partial class LoggingService : ObservableObject, ILoggingService
{
    private readonly string _logFilePath;
    private readonly object _lock = new();
    private readonly ProjectSettings _settings;

    public ObservableCollection<LogEntry> Events { get; } = new();

    [ObservableProperty]
    private ToastManager _toastManager;

    public LoggingService(ToastManager toastManager,
        ProjectModel projectModel)
    {
        ToastManager = toastManager;
        _settings = projectModel.Settings;

        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ATLab");

        Directory.CreateDirectory(dir);

        _logFilePath = Path.Combine(dir, "events.log");
    }

    public void Info(string message)
        => Log(LogLevel.INFO, message);

    public void Success(string message)
        => Log(LogLevel.SUCCESS, message);

    public void Warning(string message)
        => Log(LogLevel.WARNING, message);

    public void Error(
        string message,
        [CallerFilePath] string file = "",
        [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0)
        => Log(LogLevel.ERROR, message, file, member, line);

    private void Log(
        LogLevel level,
        string message,
        string? file = null,
        string? member = null,
        int? line = null)
    {
        var entry = new LogEntry(
            DateTime.Now,
            level,
            message,
            file is not null ? Path.GetFileName(file) : null,
            member,
            line);

        ShowToast(level, message);

        Events.Insert(0, entry);

        lock (_lock)
        {
            try
            {
                File.AppendAllText(
                    _logFilePath,
                    FormatLogEntry(entry) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Events.Insert(0,
                    new LogEntry(
                        DateTime.Now,
                        LogLevel.ERROR,
                        $"Could not write to log file: {ex.Message}"));
            }
        }

        EventAdded?.Invoke(this, EventArgs.Empty);
    }

    private void ShowToast(LogLevel level, string message)
    {
        if (level < _settings.ToastLogLevel)
            return;

        var toast = ToastManager.CreateToast(message)
            .WithContent(
                DateTime.Now.ToString(
                    "dddd, dd MMMM yyyy HH:mm",
                    CultureInfo.GetCultureInfo("en-US")))
            .OnBottomLeft()
            .WithDelay(5);

        switch (level)
        {
            case LogLevel.SUCCESS:
                toast.ShowSuccess();
                break;

            case LogLevel.WARNING:
                toast.ShowWarning();
                break;

            case LogLevel.ERROR:
                toast.ShowError();
                break;

            default:
                toast.Show();
                break;
        }
    }

    private static string FormatLogEntry(LogEntry entry)
    {
        return entry.DisplayText;
    }

    public event EventHandler? EventAdded;
}