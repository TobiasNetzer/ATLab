using ATLab.ViewModels;
using Avalonia.Controls;
using System;
using Avalonia;

namespace ATLab.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        DataContextChanged += AboutWindow_DataContextChanged;

    }

    private void AboutWindow_DataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is AboutWindowViewModel vm)
        {
            vm.RequestClose += () => Close();
        }
    }
}