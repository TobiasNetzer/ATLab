using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;
}
