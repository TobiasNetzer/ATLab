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
    private string _tolerance = "10";

    public ConfigTabViewModel(TestHardwareRelayChannelsViewModel testHardwareRelayChannelsViewModel,
        TestStepConfiguratorViewModel testStepConfiguratorViewModel)
    {
        TestHardwareRelayChannels = testHardwareRelayChannelsViewModel;
        _testStepConfiguratorViewModel = testStepConfiguratorViewModel;

        Title = "Config";
    }

    partial void OnToleranceChanged(string value)
    {
        if (double.TryParse(value, out var tolerance))
        {
            _testStepConfiguratorViewModel.Tolerance = tolerance / 100.0;
        }
    }
}