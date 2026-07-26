using System.Collections.ObjectModel;
using System.Linq;
using ATLab.Interfaces;
using ATLab.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class CommandEditorViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ProjectModel _projectModel;
    
    public ObservableCollection<Device> Devices => _projectModel.Devices;

    [ObservableProperty]
    private Device? _selectedDevice;
    
    [ObservableProperty]
    private ScriptCommand _command = new();
    
    [ObservableProperty]
    private string _timeoutMs = string.Empty;
    
    [ObservableProperty]
    private bool _isExpanded;
    
    private TestStep? _currentTestStep;
    
    public CommandEditorViewModel(
        ISettingsService settingsService,
        ProjectModel projectModel)
    {
        _settingsService = settingsService;
        _projectModel = projectModel;
        
        IsExpanded = settingsService.Settings.IsCommandEditorExpanded;
    }
    
    partial void OnSelectedDeviceChanged(Device? value)
    {
        if (value == null) return;
        if (_currentTestStep != null)
        {
            _currentTestStep.TargetDeviceId = value?.Id ?? string.Empty;
        }
    }
    
    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsCommandEditorExpanded = value;
    }
    
    partial void OnTimeoutMsChanged(string value)
    {
        if (int.TryParse(value, out var result))
        {
            Command.TimeoutMs = result;
        }
        else
        {
            Command.TimeoutMs = 0;
        }
        Dispatcher.UIThread.Post(UpdateStringProperties);
    }

    private void UpdateStringProperties()
    {
        if (_currentTestStep == null)
            return;

        TimeoutMs = _currentTestStep.Command.TimeoutMs.ToString();
        OnPropertyChanged(nameof(TimeoutMs));
    }
    
    public void LoadTestStep(TestStepViewModel? testStepViewModel)
    {
        _currentTestStep = testStepViewModel?.TestStep;

        if (_currentTestStep == null)
            return;
        
        SelectedDevice = Devices.FirstOrDefault(d => d.Id == _currentTestStep.TargetDeviceId);
        Command =  _currentTestStep.Command;
        UpdateStringProperties();
    }

    [RelayCommand]
    void RemoveDevice()
    {
        SelectedDevice = null;
        
        if (_currentTestStep != null)
            _currentTestStep.TargetDeviceId = string.Empty;
    }
}