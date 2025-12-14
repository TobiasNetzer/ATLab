using System;
using System.Collections.ObjectModel;

namespace ATLab.Interfaces;

public interface IErrorService
{
    void AddError(string message);
    ObservableCollection<string> Errors { get; }
    event EventHandler ErrorsChanged;
}