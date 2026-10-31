namespace ATLab.ViewModels;

/// <summary>Documentation tab: information about the device under test, images and attachments of the project.</summary>
public class DocumentationTabViewModel : ViewModelBase
{
    public ProjectDocumentationViewModel ProjectDocumentationViewModel { get; }
    public DeviceUnderTestInfoPanelViewModel DeviceUnderTestInfoPanelViewModel { get; }

    public DocumentationTabViewModel(
        ProjectDocumentationViewModel projectDocumentationViewModel,
        DeviceUnderTestInfoPanelViewModel deviceUnderTestInfoPanelViewModel)
    {
        ProjectDocumentationViewModel = projectDocumentationViewModel;
        DeviceUnderTestInfoPanelViewModel = deviceUnderTestInfoPanelViewModel;
        SetLocalizedTitle("Tab_Documentation");
    }

}