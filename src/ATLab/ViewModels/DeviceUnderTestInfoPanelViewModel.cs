using ATLab.Models;

namespace ATLab.ViewModels;

/// <summary>Information about the device under test shown on the Documentation tab.</summary>
public class DeviceUnderTestInfoPanelViewModel
{
    public DeviceUnderTestInfo DeviceUnderTestInfo { get; set; }
    
    public DeviceUnderTestInfoPanelViewModel(ProjectModel projectModel)
    {
        DeviceUnderTestInfo = projectModel.DeviceUnderTestInfo;
    }
}