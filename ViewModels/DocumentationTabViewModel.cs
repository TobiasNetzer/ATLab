namespace ATLab.ViewModels;

public class DocumentationTabViewModel : ViewModelBase
{
    public ProjectDocumentationViewModel ProjectDocumentationViewModel { get; }

    public DocumentationTabViewModel(ProjectDocumentationViewModel projectDocumentationViewModel)
    {
        ProjectDocumentationViewModel = projectDocumentationViewModel;
        Title = "Documentation";
    }

}