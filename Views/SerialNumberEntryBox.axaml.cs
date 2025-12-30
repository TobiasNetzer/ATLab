using System;
using Avalonia.Controls;
using ATLab.ViewModels;

namespace ATLab.Views;

public partial class SerialNumberEntryBox : Window
{
    public SerialNumberEntryBox()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SerialNumberEntryBoxViewModel vm)
        {
            vm.RequestClose += result => Close(result);
        }
    }
}