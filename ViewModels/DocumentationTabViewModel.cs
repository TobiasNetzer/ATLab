namespace ATLab.ViewModels;

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
        Title = "Documentation";
    }

}