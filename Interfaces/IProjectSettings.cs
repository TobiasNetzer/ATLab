using System;
using ATLab.Enums;

namespace ATLab.Interfaces;

public interface IProjectSettings
{
    double ToleranceValue { get; set; }
    int ResultPrecision { get; set; }
    bool UseSerialNumber { get; set; }
    bool SaveTestResult { get; set; }
    SaveTestResultOptions SaveTestResultOptions { get; set; }
    string SaveTestResultFilePath { get; set; }
    
    event Action? SettingsChanged;

    void ResetToDefault();
}
