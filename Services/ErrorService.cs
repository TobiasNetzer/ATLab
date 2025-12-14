using System;
using System.Collections.ObjectModel;
using ATLab.Interfaces;

namespace ATLab.Services;

public class ErrorService : IErrorService
{
    public ObservableCollection<string> Errors { get; } = new();

    public void AddError(string message)
    {
        Errors.Add(message);
        ErrorsChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? ErrorsChanged;
}