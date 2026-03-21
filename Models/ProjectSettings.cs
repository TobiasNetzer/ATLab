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
    private bool _useSerialNumber = false;

    partial void OnUseSerialNumberChanged(bool value) => OnSettingsChanged();

    [ObservableProperty]
    private bool _saveTestResult = false;

    partial void OnSaveTestResultChanged(bool value) => OnSettingsChanged();

    [ObservableProperty]
    private SaveTestResultOptions _saveTestResultOptions = SaveTestResultOptions.ALWAYS;

    partial void OnSaveTestResultOptionsChanged(SaveTestResultOptions value) => OnSettingsChanged();

    [ObservableProperty]
    private string _saveTestResultFilePath = string.Empty;

    partial void OnSaveTestResultFilePathChanged(string value) => OnSettingsChanged();

    public void ResetToDefault()
    {
        ToleranceValue = 10;
        ResultPrecision = 3;
        UseSerialNumber = false;
        SaveTestResult = false;
        SaveTestResultOptions = SaveTestResultOptions.ALWAYS;
        SaveTestResultFilePath = string.Empty;

        OnSettingsChanged();
    }
    
    public void CopyFrom(ProjectSettings other)
    {
        ToleranceValue = other.ToleranceValue;
        ResultPrecision = other.ResultPrecision;
        UseSerialNumber = other.UseSerialNumber;
        SaveTestResult = other.SaveTestResult;
        SaveTestResultOptions = other.SaveTestResultOptions;
        SaveTestResultFilePath = other.SaveTestResultFilePath;
    }


    private void OnSettingsChanged() => SettingsChanged?.Invoke();
}