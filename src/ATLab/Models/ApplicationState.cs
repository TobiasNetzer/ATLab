using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>Runtime state shared across the application, e.g. whether ATLab runs without test hardware.</summary>
public partial class ApplicationState : ObservableObject
{
    [ObservableProperty]
    private bool isSimulationMode;

    /// <summary>The project file being opened, null otherwise. The main window shows a progress dialog meanwhile.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoadingProject), nameof(LoadingProjectName))]
    private string? loadingProjectPath;

    public bool IsLoadingProject => LoadingProjectPath != null;

    public string LoadingProjectName => LoadingProjectPath is null ? string.Empty : Path.GetFileName(LoadingProjectPath);
}
