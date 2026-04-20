using System;
using ATLab.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class ProjectSettings : ObservableObject
{
    public event Action? SettingsChanged;

    [ObservableProperty]
    private double _toleranceValue = 10;

    partial void OnToleranceValueChanged(double value) => OnSettingsChanged();

    [ObservableProperty]
    private int _resultPrecision = 3;

    partial void OnResultPrecisionChanged(int value) => OnSettingsChanged();

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
    private SaveTestResultOptions _saveTestResultOptions = SaveTestResultOptions.ALWAYS;

    partial void OnSaveTestResultOptionsChanged(SaveTestResultOptions value) => OnSettingsChanged();

    [ObservableProperty]
    private string _saveTestResultFilePath = string.Empty;

    partial void OnSaveTestResultFilePathChanged(string value) => OnSettingsChanged();
    
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

    public void ResetToDefault()
    {
        ToleranceValue = 10;
        ResultPrecision = 3;
        IsUseSerialNumber = false;
        IsSaveTestResult = false;
        IsExportCsv = false;
        IsExportPdf = false;
        SaveTestResultOptions = SaveTestResultOptions.ALWAYS;
        SaveTestResultFilePath = string.Empty;
        IsEnableSerialNumberValidation = false;
        SerialNumberValidationLength = 0;
        SerialNumberValidationStartsWith = string.Empty;
        SerialNumberValidationEndsWith = string.Empty;
        SerialNumberValidationContains = string.Empty;

        OnSettingsChanged();
    }
    
    public void CopyFrom(ProjectSettings other)
    {
        ToleranceValue = other.ToleranceValue;
        ResultPrecision = other.ResultPrecision;
        IsUseSerialNumber = other.IsUseSerialNumber;
        IsSaveTestResult = other.IsSaveTestResult;
        IsExportCsv = other.IsExportCsv;
        IsExportPdf = other.IsExportPdf;
        SaveTestResultOptions = other.SaveTestResultOptions;
        SaveTestResultFilePath = other.SaveTestResultFilePath;
        IsEnableSerialNumberValidation = other.IsEnableSerialNumberValidation;
        SerialNumberValidationLength = other.SerialNumberValidationLength;
        SerialNumberValidationStartsWith = other.SerialNumberValidationStartsWith;
        SerialNumberValidationEndsWith = other.SerialNumberValidationEndsWith;
        SerialNumberValidationContains = other.SerialNumberValidationContains;
    }


    private void OnSettingsChanged() => SettingsChanged?.Invoke();
}