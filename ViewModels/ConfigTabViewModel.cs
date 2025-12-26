using ATLab.Interfaces;
using ATLab.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class ConfigTabViewModel : ViewModelBase
{
    
    public TestHardwareRelayChannelsViewModel TestHardwareRelayChannels { get; set; }
    
    private readonly TestStepConfiguratorViewModel _testStepConfiguratorViewModel;
    
    [ObservableProperty]
    private double _toleranceValue = 10.0;

    public ConfigTabViewModel(TestHardwareRelayChannelsViewModel testHardwareRelayChannelsViewModel,
        TestStepConfiguratorViewModel testStepConfiguratorViewModel)
    {
        TestHardwareRelayChannels = testHardwareRelayChannelsViewModel;
        _testStepConfiguratorViewModel = testStepConfiguratorViewModel;

        Title = "Config";
    }

    partial void OnToleranceValueChanged(double value)
    {
        _testStepConfiguratorViewModel.Tolerance = value / 100.0;
    }
}