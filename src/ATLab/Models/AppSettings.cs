using Avalonia.Controls;

namespace ATLab.Models;

/// <summary>Personal settings of the user, stored in settings.json and not part of any project.</summary>
public class AppSettings
{
    public double WindowWidth { get; set; } = 800;
    public double WindowHeight { get; set; } = 600;
    public double WindowX { get; set; } = -1;
    public double WindowY { get; set; } = -1;
    public WindowState WindowState { get; set; } = WindowState.Normal;
    public string? LastComPort { get; set; } = "";
    public string LastOpenedFile { get; set; } = "";
    public bool IsDarkMode { get; set; } = true;
    public string Language { get; set; } = "en";
    public bool IsForceSimulation { get; set; }
    public bool IsDevelopmentMode { get; set; }
    public bool IsDevelopmentModePasswordEnabled { get; set; }
    public string DevelopmentModePasswordHash { get; set; } = "";
    public string DevelopmentModePasswordSalt { get; set; } = "";
    public bool IsShowMeasurementPanel { get; set; }
    public bool IsStepConfiguratorExpanded { get; set; }
    public bool IsMeasSelectorExpanded { get; set; }
    public bool IsStimSelectorExpanded { get; set; }
    public bool IsExtStimSelectorExpanded { get; set; }
    public bool IsScriptSelectorExpanded { get; set; }
    public bool IsCommandEditorExpanded  { get; set; }
    public bool IsExternalProgramEditorExpanded { get; set; }
    public bool IsResponseMaskEditorExpanded { get; set; }
    public bool IsExpressionEditorExpanded { get; set; }
    public bool IsFilePathEditorExpanded { get; set; }
    public bool IsBoundaryScanEditorExpanded { get; set; }
    public bool IsBoundaryScanBusEditorExpanded { get; set; }
}