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

        OnSettingsChanged();
    }
    
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