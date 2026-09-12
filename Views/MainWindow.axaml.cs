using System;
using Avalonia.Controls;
using Avalonia;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;
using ShadUI;
using Window = ShadUI.Window;

namespace ATLab.Views;

public partial class MainWindow : Window
{
    private readonly ISettingsService? _settingsService;
    private readonly IProjectDocumentService? _projectDocumentService;
    private readonly ProjectModel? _projectModel;

    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_Closing;
    }

    public MainWindow(ISettingsService settingsService, IProjectDocumentService projectDocumentService, ProjectModel projectModel) : this()
    {
        _settingsService = settingsService;
        _projectDocumentService = projectDocumentService;
        _projectModel = projectModel;

        var s = _settingsService.Settings;

        var screens = Screens.Primary;
        var screenWidth = screens?.Bounds.Width ?? 800;
        var screenHeight = screens?.Bounds.Height ?? 600;

        Width = Math.Min(s?.WindowWidth > 0 ? s.WindowWidth : Width, screenWidth);
        Height = Math.Min(s?.WindowHeight > 0 ? s.WindowHeight : Height, screenHeight);

        var windowX = s?.WindowX ?? 0;
        var windowY = s?.WindowY ?? 0;

        Position = new PixelPoint(
            (int)Math.Max(0, Math.Min(windowX, screenWidth - Width)),
            (int)Math.Max(0, Math.Min(windowY, screenHeight - Height))
        );

        if (s != null)
        {
            WindowState = s.WindowState;
        }

    }

    public MainWindow(MainWindowViewModel vm, ISettingsService settingsService, IProjectDocumentService projectDocumentService, ProjectModel projectModel) : this(settingsService, projectDocumentService, projectModel)
    {
        DataContext = vm;
    }

    private async void MainWindow_Closing(
        object? sender,
        System.ComponentModel.CancelEventArgs e)
    {

        if (_projectDocumentService != null &&
            _projectModel != null &&
            _projectModel.IsDirty)
        {
            e.Cancel = true;

            var shouldClose =
                await _projectDocumentService.ConfirmAndContinueIfDirtyAsync();

            if (shouldClose)
            {
                Close();
            }

            return;
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
    
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        
        if (this.FindControl<DialogHost>("PART_DialogHost") is { } dialogHost)
        {
            dialogHost.Dispose();
        }
        
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        DataContext = null;
    }
}