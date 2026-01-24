using System;
using ATLab.Enums;
using ATLab.Interfaces;

namespace ATLab.Services;

public class ProjectSettingsService : IProjectSettings
{
    private double _toleranceValue = 10;
    private int _resultPrecision = 3;
    private bool _useSerialNumber = false;
    private bool _saveTestResult = false;
    private SaveTestResultOptions _saveTestResultOptions = SaveTestResultOptions.ALWAYS;
    private string _saveTestResultFilePath = string.Empty;

    public event Action? SettingsChanged;

    public double ToleranceValue
    {
        get => _toleranceValue;
        set { _toleranceValue = value; OnSettingsChanged(); }
    }

    public int ResultPrecision
    {
        get => _resultPrecision;
        set { _resultPrecision = value; OnSettingsChanged(); }
    }

    public bool UseSerialNumber
    {
        get => _useSerialNumber;
        set { _useSerialNumber = value; OnSettingsChanged(); }
    }

    public bool SaveTestResult
    {
        get => _saveTestResult;
        set { _saveTestResult = value; OnSettingsChanged(); }
    }

    public SaveTestResultOptions SaveTestResultOptions
    {
        get => _saveTestResultOptions;
        set { _saveTestResultOptions = value; OnSettingsChanged(); }
    }

    public string SaveTestResultFilePath
    {
        get => _saveTestResultFilePath;
        set { _saveTestResultFilePath = value; OnSettingsChanged(); }
    }

    public void ResetToDefault()
    {
        _toleranceValue = 10;
        _resultPrecision = 3;
        _useSerialNumber = false;
        _saveTestResultFilePath = string.Empty;
        _saveTestResult = false;
        _saveTestResultOptions = SaveTestResultOptions.ALWAYS;
        OnSettingsChanged();
    }

    private void OnSettingsChanged() => SettingsChanged?.Invoke();
}
