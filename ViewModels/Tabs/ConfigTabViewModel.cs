using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels.Tabs;

public partial class ConfigTabViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Config";
    
    public TestHardwareRelayChannelsViewModel TestHardwareRelayChannels { get; }

    public ConfigTabViewModel(TestHardwareRelayChannelsViewModel testHardwareRelayChannelsViewModel)
    {
        TestHardwareRelayChannels = testHardwareRelayChannelsViewModel;
    }
}