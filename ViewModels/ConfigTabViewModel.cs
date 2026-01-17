using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class ConfigTabViewModel : ViewModelBase
{
    
    public TestHardwareRelayChannelsViewModel TestHardwareRelayChannels { get; set; }
    
    public DeviceManagerViewModel DeviceManager { get; }
    
    public ProjectSettingsViewModel ProjectSettingsViewModel { get; }

    public ConfigTabViewModel(
        TestHardwareRelayChannelsViewModel testHardwareRelayChannelsViewModel,
        DeviceManagerViewModel deviceManagerViewModel,
        ProjectSettingsViewModel projectSettingsViewModel)
    {
        TestHardwareRelayChannels = testHardwareRelayChannelsViewModel;
        DeviceManager = deviceManagerViewModel;
        ProjectSettingsViewModel = projectSettingsViewModel;

        Title = "Config";
    }
}