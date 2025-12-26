using ATLab.Interfaces;
using ATLab.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class ConfigTabViewModel : ViewModelBase
{
    
    public TestHardwareRelayChannelsViewModel TestHardwareRelayChannels { get; set; }
    
    public SerialDeviceManagerViewModel SerialDeviceManager { get; }
    
    private readonly TestStepConfiguratorViewModel _testStepConfiguratorViewModel;
    
    [ObservableProperty]
    private double _toleranceValue = 10.0;

    public ConfigTabViewModel(TestHardwareRelayChannelsViewModel testHardwareRelayChannelsViewModel,
        TestStepConfiguratorViewModel testStepConfiguratorViewModel,
        SerialDeviceManagerViewModel serialDeviceManagerViewModel)
    {
        TestHardwareRelayChannels = testHardwareRelayChannelsViewModel;
        _testStepConfiguratorViewModel = testStepConfiguratorViewModel;
        SerialDeviceManager = serialDeviceManagerViewModel;

        Title = "Config";
    }

    partial void OnToleranceValueChanged(double value)
    {
        _testStepConfiguratorViewModel.Tolerance = value / 100.0;
    }
}