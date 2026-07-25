using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class TestInterfaceCommunicationViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    
    public List<CommunicationInterfaceType> InterfaceTypes { get; } = Enum.GetValues<CommunicationInterfaceType>().ToList();
    public List<SerialTerminationMode> SerialTerminationModes { get; } = Enum.GetValues<SerialTerminationMode>().ToList();
    
    public List<int> AvailableBaudRates { get; } = [110, 300, 600, 1200, 2400, 4800, 9600, 14400, 19200, 28800, 38400, 56000, 57600, 115200, 128000, 230400, 256000, 460800, 500000, 576000, 921600];
    public List<SerialParity> SerialParities { get; } = Enum.GetValues<SerialParity>().ToList();
    public List<int> AvailableDataBits { get; } = [7, 8];
    public List<SerialStopBits> SerialStopBits { get; } = [Enums.SerialStopBits.ONE, Enums.SerialStopBits.TWO];
    
    public List<I2CSpeedMode> I2CSpeedModes { get; } =
        Enum.GetValues<I2CSpeedMode>()
            .Where(v =>
            {
                var fi = typeof(I2CSpeedMode).GetField(v.ToString());
                return fi!.GetCustomAttributes(typeof(DescriptionAttribute), false).Length != 0;
            })
            .ToList();
    
    [ObservableProperty]
    private TestInterfaceConfig _config = new();
    
    [ObservableProperty]
    private string _timeoutMs = string.Empty;
    
    [ObservableProperty]
    private string _i2CAddress = string.Empty;
    
    [ObservableProperty]
    private string _bytesToRead = string.Empty;
    
    [ObservableProperty]
    private bool _isExpanded;
    
    private TestStep? _currentTestStep;
    
    public TestInterfaceCommunicationViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        
        IsExpanded = settingsService.Settings.IsFilePathEditorExpanded;
    }
    
    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsFilePathEditorExpanded = value;
    }
    
    partial void OnTimeoutMsChanged(string value)
    {
        if (int.TryParse(value, out var result))
        {
            Config.TimeoutMs = result;
        }
        else
        {
            Config.TimeoutMs = 0;
        }
        Dispatcher.UIThread.Post(UpdateStringProperties);
    }
    
    partial void OnI2CAddressChanged(string value)
    {
        if (int.TryParse(value, out var result) && result > 0 && result < 127)
        {
            Config.I2CAddress = result;
        }
        else
        {
            Config.I2CAddress = 0;
        }
        Dispatcher.UIThread.Post(UpdateStringProperties);
    }
    
    partial void OnBytesToReadChanged(string value)
    {
        if (int.TryParse(value, out var result))
        {
            Config.BytesToRead = result;
        }
        else
        {
            Config.BytesToRead = 0;
        }
        Dispatcher.UIThread.Post(UpdateStringProperties);
    }
    
    private void UpdateStringProperties()
    {
        if (_currentTestStep == null)
            return;

        TimeoutMs = _currentTestStep.InterfaceConfig.TimeoutMs.ToString();
        I2CAddress = _currentTestStep.InterfaceConfig.I2CAddress.ToString();
        BytesToRead = _currentTestStep.InterfaceConfig.BytesToRead.ToString();
        OnPropertyChanged(nameof(TimeoutMs));
        OnPropertyChanged(nameof(I2CAddress));
        OnPropertyChanged(nameof(BytesToRead));
    }
    
    public void LoadTestStep(TestStepViewModel? testStepViewModel)
    {
        _currentTestStep = testStepViewModel?.TestStep;

        if (_currentTestStep == null)
            return;
        
        Config =  _currentTestStep.InterfaceConfig;
        UpdateStringProperties();
    }
    
}