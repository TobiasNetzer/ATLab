using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class LabTabViewModel : ViewModelBase
{
    [ObservableProperty]
    private string title = "Lab";
}