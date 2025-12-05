using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels.Tabs;

public partial class TestingTabViewModel : ViewModelBase
{
    [ObservableProperty]
    private string title = "Testing";
}