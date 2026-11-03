using System;
using ATLab.Enums;
using ATLab.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>
/// Settings stored in the project: tolerance, serial number handling, result export, control module and log level.
/// </summary>
public partial class ProjectSettings : ObservableObject
{
    public event Action? SettingsChanged;
    public event Action? ControlModuleSettingChanged;

    [ObservableProperty]
    private double _toleranceValue = 10;

    partial void OnToleranceValueChanged(double value) => OnSettingsChanged();

    [ObservableProperty]
    private int _displayedDecimalPlaces = 3;

    partial void OnDisplayedDecimalPlacesChanged(int value) => OnSettingsChanged();

    [ObservableProperty]
    private bool _isUseSerialNumber;

    partial void OnIsUseSerialNumberChanged(bool value) => OnSettingsChanged();

    [ObservableProperty]
    private bool _isSaveTestResult;

    partial void OnIsSaveTestResultChanged(bool value) => OnSettingsChanged();
    
    [ObservableProperty]
    private bool _isExportCsv;
    
    partial void OnIsExportCsvChanged(bool value) => OnSettingsChanged();
    
    [ObservableProperty]
    private bool _isExportPdf;
    
    partial void OnIsExportPdfChanged(bool value) => OnSettingsChanged();

    [ObservableProperty]
    private bool _isExportJson;

    partial void OnIsExportJsonChanged(bool value) => OnSettingsChanged();

    // Writes passed runs to <output path>\Pass and failed runs to <output path>\Fail
    [ObservableProperty]
    private bool _isSortResultsByVerdict;

    partial void OnIsSortResultsByVerdictChanged(bool value) => OnSettingsChanged();

    [ObservableProperty]
    private SaveTestResultOptions _saveTestResultOptions = SaveTestResultOptions.ALWAYS;

    partial void OnSaveTestResultOptionsChanged(SaveTestResultOptions value) => OnSettingsChanged();

    // Output folder of each export format
    [ObservableProperty]
    private string _csvOutputPath = string.Empty;

    partial void OnCsvOutputPathChanged(string value) => OnSettingsChanged();

    [ObservableProperty]
    private string _jsonOutputPath = string.Empty;

    partial void OnJsonOutputPathChanged(string value) => OnSettingsChanged();

    [ObservableProperty]
    private string _pdfOutputPath = string.Empty;

    partial void OnPdfOutputPathChanged(string value) => OnSettingsChanged();

    [ObservableProperty]
    private bool _isEnableSerialNumberValidation;
    
    partial void OnIsEnableSerialNumberValidationChanged(bool value) => OnSettingsChanged();
    
    [ObservableProperty]
    private int _serialNumberValidationLength;
    
    partial void OnSerialNumberValidationLengthChanged(int value) => OnSettingsChanged();
    
    [ObservableProperty]
    private string _serialNumberValidationStartsWith = string.Empty;

    partial void OnSerialNumberValidationStartsWithChanged(string value) => OnSettingsChanged();
    
    [ObservableProperty]
    private string _serialNumberValidationEndsWith = string.Empty;

    partial void OnSerialNumberValidationEndsWithChanged(string value) => OnSettingsChanged();
    
    [ObservableProperty]
    private string _serialNumberValidationContains = string.Empty;

    partial void OnSerialNumberValidationContainsChanged(string value) => OnSettingsChanged();

    [ObservableProperty]
    private bool _isControlModuleEnabled;
    
    [ObservableProperty]
    private LogLevel _toastLogLevel = LogLevel.INFO;

    /// <summary>Adds a drawing of the board with the nets of every failed step to the PDF test report.</summary>
    [ObservableProperty]
    private bool _isIncludeBoardViewInReport;

    partial void OnIsIncludeBoardViewInReportChanged(bool value) => OnSettingsChanged();

    /// <summary>
    /// KiCad layers drawn when a board picture of the report shows the top side ("@PadsTop", "@PadsBottom" and
    /// "@Holes" for pads and holes); null for the default, the top pads.
    /// </summary>
    [ObservableProperty]
    private System.Collections.Generic.List<string>? _reportBoardLayers;

    partial void OnReportBoardLayersChanged(System.Collections.Generic.List<string>? value) => OnSettingsChanged();

    /// <summary>
    /// Layers drawn when a board picture shows the bottom side; null for the default, the bottom pads.
    /// </summary>
    [ObservableProperty]
    private System.Collections.Generic.List<string>? _reportBoardLayersBottom;

    partial void OnReportBoardLayersBottomChanged(System.Collections.Generic.List<string>? value) => OnSettingsChanged();

    /// <summary>
    /// Opacity in percent of layers in the board pictures of the report, for layers the user changed; the others use
    /// their default (copper faint, silkscreen strong).
    /// </summary>
    [ObservableProperty]
    private System.Collections.Generic.Dictionary<string, int>? _reportBoardLayerOpacity;

    partial void OnReportBoardLayerOpacityChanged(System.Collections.Generic.Dictionary<string, int>? value) => OnSettingsChanged();

    /// <summary>
    /// Order of the layers in the board pictures of the report, the topmost first ("@Pads" and "@Holes" included);
    /// null for the automatic order (the viewed side over the far side, pads and holes on top).
    /// </summary>
    [ObservableProperty]
    private System.Collections.Generic.List<string>? _reportBoardLayerOrder;

    partial void OnReportBoardLayerOrderChanged(System.Collections.Generic.List<string>? value) => OnSettingsChanged();

    /// <summary>Colors ("#RRGGBB") of layers in the board pictures of the report the user changed; the others are black.</summary>
    [ObservableProperty]
    private System.Collections.Generic.Dictionary<string, string>? _reportBoardLayerColors;

    partial void OnReportBoardLayerColorsChanged(System.Collections.Generic.Dictionary<string, string>? value) => OnSettingsChanged();

    /// <summary>Side the board pictures of the report show; automatic: the side with more of the step's copper.</summary>
    [ObservableProperty]
    private ReportBoardSide _reportBoardSide;

    partial void OnReportBoardSideChanged(ReportBoardSide value) => OnSettingsChanged();
    
    partial void OnToastLogLevelChanged(LogLevel value) => OnSettingsChanged();

    partial void OnIsControlModuleEnabledChanged(bool value)
    {
        ControlModuleSettingChanged?.Invoke();
        OnSettingsChanged();
    }

    public void ResetToDefault()
    {
        ToleranceValue = 10;
        DisplayedDecimalPlaces = 3;
        IsUseSerialNumber = false;
        IsSaveTestResult = false;
        IsExportCsv = false;
        IsExportPdf = false;
        IsExportJson = false;
        IsSortResultsByVerdict = false;
        SaveTestResultOptions = SaveTestResultOptions.ALWAYS;
        CsvOutputPath = string.Empty;
        JsonOutputPath = string.Empty;
        PdfOutputPath = string.Empty;
        IsEnableSerialNumberValidation = false;
        SerialNumberValidationLength = 0;
        SerialNumberValidationStartsWith = string.Empty;
        SerialNumberValidationEndsWith = string.Empty;
        SerialNumberValidationContains = string.Empty;
        IsControlModuleEnabled = false;
        ToastLogLevel = LogLevel.INFO;
        IsIncludeBoardViewInReport = false;
        ReportBoardLayers = null;
        ReportBoardLayersBottom = null;
        ReportBoardLayerOpacity = null;
        ReportBoardLayerOrder = null;
        ReportBoardLayerColors = null;
        ReportBoardSide = ReportBoardSide.Automatic;

        OnSettingsChanged();
    }
    
    /// <summary>The report layers of the project as the shared layer settings (copies).</summary>
    public ATLab.Pcb.Rendering.ReportLayerSettings ReportLayers()
    {
        var settings = new ATLab.Pcb.Rendering.ReportLayerSettings
        {
            Opacity = ReportBoardLayerOpacity is null ? null : new(ReportBoardLayerOpacity),
            Order = ReportBoardLayerOrder is null ? null : [.. ReportBoardLayerOrder],
            Colors = ReportBoardLayerColors is null ? null : new(ReportBoardLayerColors)
        };
        if (ReportBoardLayers is not null)
            settings.SetLayers(TopView, [.. ReportBoardLayers]);
        if (ReportBoardLayersBottom is not null)
            settings.SetLayers(BottomView, [.. ReportBoardLayersBottom]);
        return settings;
    }

    /// <summary>Keys of the two views of the report's board pictures in the shared layer settings.</summary>
    public const string TopView = "top";

    public const string BottomView = "bottom";

    public ProjectSettings Clone()
    {
        var clone = new ProjectSettings();
        clone.CopyFrom(this);
        return clone;
    }
    
    public void CopyFrom(ProjectSettings other)
    {
        ToleranceValue = other.ToleranceValue;
        DisplayedDecimalPlaces = other.DisplayedDecimalPlaces;
        IsUseSerialNumber = other.IsUseSerialNumber;
        IsSaveTestResult = other.IsSaveTestResult;
        IsExportCsv = other.IsExportCsv;
        IsExportPdf = other.IsExportPdf;
        IsExportJson = other.IsExportJson;
        IsSortResultsByVerdict = other.IsSortResultsByVerdict;
        SaveTestResultOptions = other.SaveTestResultOptions;
        CsvOutputPath = other.CsvOutputPath;
        JsonOutputPath = other.JsonOutputPath;
        PdfOutputPath = other.PdfOutputPath;
        IsEnableSerialNumberValidation = other.IsEnableSerialNumberValidation;
        SerialNumberValidationLength = other.SerialNumberValidationLength;
        SerialNumberValidationStartsWith = other.SerialNumberValidationStartsWith;
        SerialNumberValidationEndsWith = other.SerialNumberValidationEndsWith;
        SerialNumberValidationContains = other.SerialNumberValidationContains;
        IsControlModuleEnabled = other.IsControlModuleEnabled;
        ToastLogLevel = other.ToastLogLevel;
        IsIncludeBoardViewInReport = other.IsIncludeBoardViewInReport;
        ReportBoardLayers = other.ReportBoardLayers is null ? null : [.. other.ReportBoardLayers];
        ReportBoardLayersBottom = other.ReportBoardLayersBottom is null ? null : [.. other.ReportBoardLayersBottom];
        ReportBoardLayerOpacity = other.ReportBoardLayerOpacity is null ? null : new(other.ReportBoardLayerOpacity);
        ReportBoardLayerOrder = other.ReportBoardLayerOrder is null ? null : [.. other.ReportBoardLayerOrder];
        ReportBoardLayerColors = other.ReportBoardLayerColors is null ? null : new(other.ReportBoardLayerColors);
        ReportBoardSide = other.ReportBoardSide;
    }

    public void PrepareForSave(PathService pathService)
    {
        CsvOutputPath = pathService.ToRelative(CsvOutputPath);
        JsonOutputPath = pathService.ToRelative(JsonOutputPath);
        PdfOutputPath = pathService.ToRelative(PdfOutputPath);
    }

    public void RestoreAfterLoad(PathService pathService)
    {
        CsvOutputPath = pathService.ToAbsolute(CsvOutputPath);
        JsonOutputPath = pathService.ToAbsolute(JsonOutputPath);
        PdfOutputPath = pathService.ToAbsolute(PdfOutputPath);
    }
    
    private void OnSettingsChanged() => SettingsChanged?.Invoke();
}