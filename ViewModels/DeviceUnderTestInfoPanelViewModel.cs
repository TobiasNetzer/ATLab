using ATLab.Models;

namespace ATLab.ViewModels;

public class DeviceUnderTestInfoPanelViewModel
{
    public DeviceUnderTestInfo DeviceUnderTestInfo { get; set; }
    
    public DeviceUnderTestInfoPanelViewModel(ProjectModel projectModel)
    {
        DeviceUnderTestInfo = projectModel.DeviceUnderTestInfo;
    }
}