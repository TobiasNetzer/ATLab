using System;
using System.Collections.ObjectModel;
using ATLab.Records;

namespace ATLab.Interfaces;

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
