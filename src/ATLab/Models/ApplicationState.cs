using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>Runtime state shared across the application, e.g. whether ATLab runs without test hardware.</summary>
public partial class ApplicationState : ObservableObject
{
    [ObservableProperty]
    private bool isSimulationMode;
}