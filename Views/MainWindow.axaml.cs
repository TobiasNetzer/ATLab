using System;
using Avalonia.Controls;
using Avalonia;
using ATLab.Interfaces;
using ATLab.ViewModels;

namespace ATLab.Views;

public partial class MainWindow : Window
{
    private readonly ISettingsService? _settingsService;

    private readonly IMessageBoxService? _messageBoxService;

    public MainWindow()
    {
        InitializeComponent();
        this.Closing += MainWindow_Closing;
        
        DataContextChanged += (sender, args) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.RequestClose += () => this.Close();
            }
        };
    }

    public MainWindow(ISettingsService settingsService, IMessageBoxService messageBoxService) : this()
    {
        _settingsService = settingsService;
        _messageBoxService = messageBoxService;

        var s = _settingsService.Settings;

        var screens = Screens.Primary;
        double screenWidth = screens?.Bounds.Width ?? 800;
        double screenHeight = screens?.Bounds.Height ?? 600;

        Width = Math.Min(s?.WindowWidth > 0 ? s.WindowWidth : Width, screenWidth);
        Height = Math.Min(s?.WindowHeight > 0 ? s.WindowHeight : Height, screenHeight);

        double windowX = s?.WindowX ?? 0;
        double windowY = s?.WindowY ?? 0;

        Position = new PixelPoint(
            (int)Math.Max(0, Math.Min(windowX, screenWidth - Width)),
            (int)Math.Max(0, Math.Min(windowY, screenHeight - Height))
        );

        if (s != null)
        {
            WindowState = s.WindowState;
        }

    }

    private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            if (vm.TestingTab.TestStepPresenter.IsDirty)
            {
                e.Cancel = true;

                if (_messageBoxService != null)
                {
                    var result = await _messageBoxService.ShowConfirmationAsync(
                        "Unsaved Changes",
                        "You have unsaved changes. Do you want to continue and lose your changes?");

                    if (result)
                    {
                        vm.TestingTab.TestStepPresenter.IsDirty = false;
                        Close();
                    }
                }

                return;
            }
        }

        if (_settingsService == null)
            return;

        var settings = _settingsService.Settings;
        if (WindowState == WindowState.Maximized)
        {
            settings.WindowState = WindowState.Maximized;
        }
        else
        {
            settings.WindowState = WindowState.Normal;
            settings.WindowWidth = Width;
            settings.WindowHeight = Height;
            settings.WindowX = Position.X;
            settings.WindowY = Position.Y;
        }
        _settingsService.Save();
    }
}
