using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class ConfigTabViewModel : ViewModelBase
{
    
    public TestHardwareRelayChannelsViewModel TestHardwareRelayChannels { get; }
    
    public DeviceManagerViewModel DeviceManager { get; }
    
    public ProjectSettingsViewModel ProjectSettingsViewModel { get; }
    
    public RuntimeVariableEditorViewModel RuntimeVariableEditorViewModel { get; }

    public ConfigTabViewModel(
        TestHardwareRelayChannelsViewModel testHardwareRelayChannelsViewModel,
        DeviceManagerViewModel deviceManagerViewModel,
        ProjectSettingsViewModel projectSettingsViewModel,
        RuntimeVariableEditorViewModel runtimeVariableEditorViewModel)
    {
        TestHardwareRelayChannels = testHardwareRelayChannelsViewModel;
        DeviceManager = deviceManagerViewModel;
        ProjectSettingsViewModel = projectSettingsViewModel;
        RuntimeVariableEditorViewModel = runtimeVariableEditorViewModel;

        Title = "Configuration";
    }
}