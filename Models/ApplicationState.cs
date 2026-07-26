using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class ApplicationState : ObservableObject
{
    [ObservableProperty]
    private bool isSimulationMode;
}