using System;
using Avalonia.Controls;
using Avalonia;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;
using ShadUI;
using Window = ShadUI.Window;

namespace ATLab.Views;

/// <summary>Main window. Restores its size and position and asks about unsaved changes before closing.</summary>
public partial class MainWindow : Window
{
    private readonly ISettingsService? _settingsService;
    private readonly IProjectDocumentService? _projectDocumentService;
    private readonly ProjectModel? _projectModel;
    
    private const int ShadUiWidthOffset = 16;
    private const int ShadUiHeightOffset = 9;

    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_Closing;
    }

    public MainWindow(
        ISettingsService settingsService,
        IProjectDocumentService projectDocumentService,
        ProjectModel projectModel) : this()
    {
        _settingsService = settingsService;
        _projectDocumentService = projectDocumentService;
        _projectModel = projectModel;

        var s = _settingsService.Settings;

        var screen = Screens.Primary;
        var screenWidth = screen?.Bounds.Width ?? 800;
        var screenHeight = screen?.Bounds.Height ?? 600;

        Width = Math.Min(
            (s?.WindowWidth > 0 ? s.WindowWidth : Width) - ShadUiWidthOffset,
            screenWidth);

        Height = Math.Min(
                (s?.WindowHeight > 0 ? s.WindowHeight : Height) - ShadUiHeightOffset,
                screenHeight);

        var windowX = s?.WindowX ?? 0;
        var windowY = s?.WindowY ?? 0;

        var restorePosition = new PixelPoint(
            (int)Math.Round(Math.Max(0, Math.Min(windowX, screenWidth - Width))),
            (int)Math.Round(Math.Max(0, Math.Min(windowY, screenHeight - Height)))
        );

        if (s != null)
        {
            WindowState = s.WindowState;
        }

        Opened += (_, _) =>
        {
            if (WindowState == WindowState.Normal)
            {
                Position = restorePosition;
            }
        };
    }

    public MainWindow(
        MainWindowViewModel vm,
        ISettingsService settingsService,
        IProjectDocumentService projectDocumentService,
        ProjectModel projectModel)
        : this(settingsService, projectDocumentService, projectModel)
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