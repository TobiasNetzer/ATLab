using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class TestingTabViewModel : ViewModelBase
{
    [ObservableProperty]
    private string title = "Testing";
}