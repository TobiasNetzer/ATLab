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
    private readonly IScpiScriptService _scriptService;
    private readonly ISettingsService _settingsService;
    
    public ObservableCollection<SerialDevices> Devices => _deviceManager.SerialDevices;
    public ObservableCollection<ScpiScriptItemViewModel> Scripts => _scriptService.Scripts;

    [ObservableProperty]
    private SerialDevices? _selectedDevice;

    [ObservableProperty]
    private ScpiScriptItemViewModel? _selectedScript;

    [ObservableProperty]
    private TestStep? _selectedTestStep;

    [ObservableProperty]
    private bool _isBusy;
    
    [ObservableProperty]
    private bool _isExpanded;

    private bool _isSyncing;
    
    public ScriptSelectorViewModel(
        SerialDeviceManagerViewModel deviceManager,
        IScpiScriptService scriptService,
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

    partial void OnSelectedTestStepChanged(TestStep? value)
    {
        _isSyncing = true;
        try
        {
            if (value == null)
            {
                SelectedScript = null;
                return;
            }

            SelectedScript = Scripts.FirstOrDefault(s => s.Id == value.ScriptId);
            SyncVariables();
            SelectedDevice = Devices.FirstOrDefault(d => d.Name == value.TargetDevice);
        }
        finally
        {
            _isSyncing = false;
        }
    }

    partial void OnSelectedScriptChanged(ScpiScriptItemViewModel? value)
    {
        if (_isSyncing) return;

        if (SelectedTestStep != null && value != null)
        {
            SelectedTestStep.ScriptId = value.Id;
            SyncVariables();
        }
    }

    partial void OnSelectedDeviceChanged(SerialDevices? value)
    {
        if (SelectedTestStep != null && value != null)
        {
            SelectedTestStep.TargetDevice = value.Name;
        }
    }
    
    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsScriptSelectorExpanded = value;
    }

    private void SyncVariables()
    {
        if (SelectedTestStep == null) return;

        if (SelectedScript == null)
        {
            SelectedTestStep.ScriptVariables.Clear();
            return;
        }

        var scriptVars = SelectedScript.Variables;
        var stepVars = SelectedTestStep.ScriptVariables;

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
    }

    [RelayCommand]
    void RemoveDevice()
    {
        if (SelectedTestStep == null) return;
        if (SelectedDevice == null) return;

        SelectedTestStep.TargetDevice = string.Empty;
        SelectedDevice = null;
    }

    [RelayCommand]
    void RemoveScript()
    {
        if (SelectedTestStep == null) return;
        if (SelectedScript == null) return;
        
        SelectedTestStep.ScriptId = string.Empty;
        SelectedTestStep.ScriptVariables.Clear();
        SelectedScript = null;
    }

}
