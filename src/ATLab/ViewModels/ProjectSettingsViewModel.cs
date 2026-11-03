using System;
using System.Collections.Generic;
using ATLab.Pcb.Controls;
using ATLab.Pcb.Rendering;
using ATLab.Services;
using Avalonia.Threading;
using System.Globalization;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

/// <summary>
/// Project settings editor: result export, serial number validation, tolerance, decimal places and log level.
/// </summary>
public partial class ProjectSettingsViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialogService;

    public ProjectSettings Settings { get; }
    public List<LogLevel> LogLevels { get; } = new(Enum.GetValues<LogLevel>());
    
    [ObservableProperty]
    private LogLevel _selectedLogLevel = LogLevel.INFO;
    
    [ObservableProperty]
    private string _serialNumberValidationLengthString = string.Empty;
    
    [ObservableProperty]
    private string? _toleranceString = "10";
    
    [ObservableProperty]
    private string? _displayedDecimalPlacesString = "3";

    // ----- Layers of the board pictures in the report -----

    private readonly BoardService _boardService;
    private readonly ISettingsService _settingsService;
    private bool _isApplyingReportLayers;

    /// <summary>
    /// Pads, holes and the KiCad layers of the board: drawn in the top and in the bottom view, with their order, color
    /// and opacity.
    /// </summary>
    public ReportLayerList ReportLayers { get; } = new(name => name switch
        {
            LayerColors.TopPads => LocalizationService.Instance.GetString("Board_LayerTopPads"),
            LayerColors.BottomPads => LocalizationService.Instance.GetString("Board_LayerBottomPads"),
            LayerColors.Holes => LocalizationService.Instance.GetString("Board_LayerHoles"),
            _ => name
        },
        [
            new ReportLayerView(ProjectSettings.TopView, () => LocalizationService.Instance.GetString("ReportSide_Top"), [LayerColors.TopPads],
                () => LocalizationService.Instance.GetString("ToolTip_ReportLayerTop")),
            new ReportLayerView(ProjectSettings.BottomView, () => LocalizationService.Instance.GetString("ReportSide_Bottom"), [LayerColors.BottomPads],
                () => LocalizationService.Instance.GetString("ToolTip_ReportLayerBottom"))
        ]);

    /// <summary>The list from the project's settings; a view without a choice draws the pads of its side.</summary>
    private void BuildReportLayerOptions()
    {
        var settings = Settings.ReportLayers();
        ReportLayers.UserDefault = _settingsService.Settings.DefaultReportLayers;
        var side = Settings.ReportBoardSide == ReportBoardSide.Bottom ? ATLab.Pcb.Kicad.BoardSide.Bottom : ATLab.Pcb.Kicad.BoardSide.Top;
        ReportLayers.Load(_boardService.Board, settings, side);
    }

    /// <summary>A change in the list goes into the project's settings.</summary>
    private void ApplyReportLayers()
    {
        var chosen = ReportLayers.Settings;
        _isApplyingReportLayers = true;
        try
        {
            Settings.ReportBoardLayers = chosen.LayersOf(ProjectSettings.TopView);
            Settings.ReportBoardLayersBottom = chosen.LayersOf(ProjectSettings.BottomView);
            Settings.ReportBoardLayerOpacity = chosen.Opacity;
            Settings.ReportBoardLayerOrder = chosen.Order;
            Settings.ReportBoardLayerColors = chosen.Colors;
        }
        finally
        {
            _isApplyingReportLayers = false;
        }
    }

    // ----- Side of the board pictures -----

    public bool IsReportSideAutomatic
    {
        get => Settings.ReportBoardSide == ReportBoardSide.Automatic;
        set { if (value) Settings.ReportBoardSide = ReportBoardSide.Automatic; }
    }

    public bool IsReportSideTop
    {
        get => Settings.ReportBoardSide == ReportBoardSide.Top;
        set { if (value) Settings.ReportBoardSide = ReportBoardSide.Top; }
    }

    public bool IsReportSideBottom
    {
        get => Settings.ReportBoardSide == ReportBoardSide.Bottom;
        set { if (value) Settings.ReportBoardSide = ReportBoardSide.Bottom; }
    }

    public bool CanSaveTestResult =>
        Settings.IsUseSerialNumber && Settings.IsSaveTestResult;

    public ProjectSettingsViewModel(
        IFileDialogService fileDialogService,
        ProjectModel projectModel,
        BoardService boardService,
        ISettingsService settingsService)
    {
        _fileDialogService = fileDialogService;
        _boardService = boardService;
        Settings = projectModel.Settings;

        _settingsService = settingsService;
        ReportLayers.Changed += ApplyReportLayers;
        ReportLayers.DefaultSaved += layers =>
        {
            _settingsService.Settings.DefaultReportLayers = layers;
            _settingsService.Save();
        };
        boardService.Changed += () => Dispatcher.UIThread.Post(BuildReportLayerOptions);
        Settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Settings.ReportBoardSide))
            {
                OnPropertyChanged(nameof(IsReportSideAutomatic));
                OnPropertyChanged(nameof(IsReportSideTop));
                OnPropertyChanged(nameof(IsReportSideBottom));
            }

            if (e.PropertyName is nameof(Settings.ReportBoardLayers) or nameof(Settings.ReportBoardLayersBottom) or nameof(Settings.ReportBoardLayerOpacity)
                    or nameof(Settings.ReportBoardLayerOrder) or nameof(Settings.ReportBoardLayerColors)
                    or nameof(Settings.ReportBoardSide) && !_isApplyingReportLayers)
                Dispatcher.UIThread.Post(BuildReportLayerOptions);
        };
        BuildReportLayerOptions();
        
        Settings.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(Settings.IsUseSerialNumber)
                    or nameof(Settings.IsSaveTestResult):
                    
                    OnPropertyChanged(nameof(CanSaveTestResult));
                    break;
                
                case nameof(Settings.SerialNumberValidationLength):
                    
                    SerialNumberValidationLengthString = Settings.SerialNumberValidationLength == 0
                        ? string.Empty
                        : Settings.SerialNumberValidationLength.ToString();
                    
                    break;
                
                case nameof(Settings.DisplayedDecimalPlaces):
                    
                    DisplayedDecimalPlacesString = Settings.DisplayedDecimalPlaces.ToString();
                    
                    break;
                
                case nameof(Settings.ToleranceValue):
                    
                    ToleranceString = Settings.ToleranceValue.ToString(CultureInfo.CurrentCulture);
                    
                    break;
                
                case nameof(Settings.ToastLogLevel):

                    SelectedLogLevel = Settings.ToastLogLevel;

                    break;
                
            }
        };
    }

    partial void OnToleranceStringChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        
        if (double.TryParse(value, CultureInfo.CurrentCulture, out var result) ||
            double.TryParse(value, CultureInfo.InvariantCulture, out result))
        {
            Settings.ToleranceValue = result;
        }
    }

    partial void OnDisplayedDecimalPlacesStringChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        
        Settings.DisplayedDecimalPlaces = int.Parse(value);
    }

    partial void OnSerialNumberValidationLengthStringChanged(string value)
    {
        if (int.TryParse(value, out var parsed))
            Settings.SerialNumberValidationLength = parsed;
    }
    
    partial void OnSelectedLogLevelChanged(LogLevel value)
    {
        Settings.ToastLogLevel = value;
    }

    [RelayCommand]
    private async Task SelectCsvOutputPath()
    {
        if (await SelectFolderAsync("Select folder for CSV export") is { } path)
            Settings.CsvOutputPath = path;
    }

    [RelayCommand]
    private async Task SelectJsonOutputPath()
    {
        if (await SelectFolderAsync("Select folder for JSON export") is { } path)
            Settings.JsonOutputPath = path;
    }

    [RelayCommand]
    private async Task SelectPdfOutputPath()
    {
        if (await SelectFolderAsync("Select folder for PDF export") is { } path)
            Settings.PdfOutputPath = path;
    }

    private async Task<string?> SelectFolderAsync(string title)
    {
        var result = await _fileDialogService.OpenFolderAsync(title);
        return result?.Path.LocalPath;
    }

    [RelayCommand]
    private void ResetToDefault() => Settings.ResetToDefault();
}