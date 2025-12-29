using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class ConfigTabViewModel : ViewModelBase
{
    
    public TestHardwareRelayChannelsViewModel TestHardwareRelayChannels { get; set; }
    
    public SerialDeviceManagerViewModel SerialDeviceManager { get; }
    
    private readonly TestStepConfiguratorViewModel _testStepConfiguratorViewModel;
    
    [ObservableProperty]
    private double _toleranceValue;

    public ConfigTabViewModel(TestHardwareRelayChannelsViewModel testHardwareRelayChannelsViewModel,
        TestStepConfiguratorViewModel testStepConfiguratorViewModel,
        SerialDeviceManagerViewModel serialDeviceManagerViewModel)
    {
        TestHardwareRelayChannels = testHardwareRelayChannelsViewModel;
        _testStepConfiguratorViewModel = testStepConfiguratorViewModel;
        SerialDeviceManager = serialDeviceManagerViewModel;

        Title = "Config";
        ToleranceValue = 10;
    }

    partial void OnToleranceValueChanged(double value)
    {
        _testStepConfiguratorViewModel.Tolerance = value / 100.0;
    }
}