using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class ConfigTabViewModel : ViewModelBase
{
    
    public TestHardwareRelayChannelsViewModel TestHardwareRelayChannels { get; set; }
    
    public SerialDeviceManagerViewModel SerialDeviceManager { get; }
    
    public ProjectSettingsViewModel ProjectSettingsViewModel { get; }

    public ConfigTabViewModel(
        TestHardwareRelayChannelsViewModel testHardwareRelayChannelsViewModel,
        SerialDeviceManagerViewModel serialDeviceManagerViewModel,
        ProjectSettingsViewModel projectSettingsViewModel)
    {
        TestHardwareRelayChannels = testHardwareRelayChannelsViewModel;
        SerialDeviceManager = serialDeviceManagerViewModel;
        ProjectSettingsViewModel = projectSettingsViewModel;

        Title = "Config";
    }
}