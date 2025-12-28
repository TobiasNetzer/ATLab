using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class ScriptSelectorViewModel : ViewModelBase
{
    private readonly SerialDeviceManagerViewModel _deviceManager;
    private readonly IScriptService _scriptService;
    private readonly ISettingsService _settingsService;
    
    public ObservableCollection<SerialDevices> Devices => _deviceManager.SerialDevices;
    public ObservableCollection<ScriptItemViewModel> Scripts => _scriptService.Scripts;
    public ObservableCollection<ScriptVariable> ScriptVariables { get; } = new();

    [ObservableProperty]
    private SerialDevices? _selectedDevice;

    [ObservableProperty]
    private ScriptItemViewModel? _selectedScript;

    private TestStep? _currentTestStep;

    [ObservableProperty]
    private bool _isBusy;
    
    [ObservableProperty]
    private bool _isExpanded;

    private bool _isSyncing;
    
    public ScriptSelectorViewModel(
        SerialDeviceManagerViewModel deviceManager,
        IScriptService scriptService,
        ISettingsService settingsService)
    {
        _deviceManager = deviceManager;
        _scriptService = scriptService;
        _settingsService = settingsService;
        
        IsExpanded = settingsService.Settings.IsScriptSelectorExpanded;

        if (Scripts.Count == 0)
        {
            _ = _scriptService.LoadAllAsync();
        }
    }

    public void LoadTestStep(TestStepViewModel? testStepViewModel)
    {
        _isSyncing = true;
        try
        {
            _currentTestStep = testStepViewModel?.TestStep;

            if (_currentTestStep == null)
            {
                SelectedScript = null;
                ScriptVariables.Clear();
                return;
            }

            SelectedScript = Scripts.FirstOrDefault(s => s.Id == _currentTestStep.ScriptId);
            SyncVariables();
            SelectedDevice = Devices.FirstOrDefault(d => d.Name == _currentTestStep.TargetDevice);
        }
        finally
        {
            _isSyncing = false;
        }
    }

    partial void OnSelectedScriptChanged(ScriptItemViewModel? value)
    {
        if (_isSyncing) return;

        if (_currentTestStep != null)
        {
            _currentTestStep.ScriptId = value?.Id ?? string.Empty;
            SyncVariables();
            return;
        }
        
        if (SelectedScript == null)
        {
            ScriptVariables.Clear();
            return;
        }
        
        var stepVars = SelectedScript.Variables;
        ScriptVariables.Clear();
        foreach (var v in stepVars)
        {
            ScriptVariables.Add(v);
        }
    }

    partial void OnSelectedDeviceChanged(SerialDevices? value)
    {
        if (_currentTestStep != null)
        {
            _currentTestStep.TargetDevice = value?.Name ?? string.Empty;
        }
    }
    
    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsScriptSelectorExpanded = value;
    }

    private void SyncVariables()
    {
        if (_currentTestStep == null)
        {
            ScriptVariables.Clear();
            return;
        }

        if (SelectedScript == null)
        {
            _currentTestStep.ScriptVariables.Clear();
            ScriptVariables.Clear();
            return;
        }

        var scriptVars = SelectedScript.Variables;
        var stepVars = _currentTestStep.ScriptVariables;

        // Remove variables that are no longer in the script
        var toRemove = stepVars.Where(sv => scriptVars.All(v => v.Name != sv.Name)).ToList();
        foreach (var v in toRemove) stepVars.Remove(v);

        // Add or update variables from the script
        foreach (var v in scriptVars)
        {
            var existing = stepVars.FirstOrDefault(sv => sv.Name == v.Name);
            if (existing == null)
            {
                stepVars.Add(v.Clone());
            }
            // If it exists, we keep the user's value
        }

        // Sync the local collection
        ScriptVariables.Clear();
        foreach (var v in stepVars)
        {
            ScriptVariables.Add(v);
        }
    }

    [RelayCommand]
    void RemoveDevice() => SelectedDevice = null;

    [RelayCommand]
    void RemoveScript() => SelectedScript = null;

}
