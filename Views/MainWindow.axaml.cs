using System;
using Avalonia.Controls;
using Avalonia;
using ATLab.ViewModels;

namespace ATLab.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.Closing += MainWindow_Closing;

        var s = App.SettingsService?.Settings;

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

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var settingsService = App.SettingsService;

        if (settingsService == null || settingsService.Settings == null)
            return;

        var settings = settingsService.Settings;
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
        settingsService.Save();
    }
}
