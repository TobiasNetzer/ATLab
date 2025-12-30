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
        
        Closed += (_, _) => DataContextChanged -= OnDataContextChanged;
        
        SerialNoTextBox.AttachedToVisualTree += (_, _) =>
        {
            SerialNoTextBox.Focus();
            SerialNoTextBox.CaretIndex = SerialNoTextBox.Text?.Length ?? 0;
        };
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SerialNumberEntryBoxViewModel vm)
        {
            vm.RequestClose -= OnRequestClose; 
            vm.RequestClose += OnRequestClose;
        }
    }
    
    private void OnRequestClose(bool result) => Close(result);
}