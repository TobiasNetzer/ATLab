using System;
using System.Collections.ObjectModel;

namespace ATLab.Interfaces;

public interface IErrorService
{
    void AddError(
        string message,
        [System.Runtime.CompilerServices.CallerFilePath] string file = "",
        [System.Runtime.CompilerServices.CallerMemberName] string member = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int line = 0
    );

    ObservableCollection<string> Errors { get; }
    event EventHandler ErrorsChanged;
}
