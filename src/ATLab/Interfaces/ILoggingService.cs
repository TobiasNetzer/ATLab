using System;
using System.Collections.ObjectModel;
using ATLab.Records;

namespace ATLab.Interfaces;

/// <summary>
/// Collects messages for the event log, shows them as toasts and writes them to the log file according to the log
/// level.
/// </summary>
public interface ILoggingService
{
    void Error(
        string message,
        [System.Runtime.CompilerServices.CallerFilePath] string file = "",
        [System.Runtime.CompilerServices.CallerMemberName] string member = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int line = 0
    );
    
    void Success(string message);
    void Info(string message);
    void Warning(string message);

    ObservableCollection<LogEntry> Events { get; }
    event EventHandler EventAdded;
}
