using Avalonia.Controls;

namespace ATLab.Models;

public class AppSettings
{
    public double WindowWidth { get; set; } = 800;
    public double WindowHeight { get; set; } = 600;
    public double WindowX { get; set; } = -1;
    public double WindowY { get; set; } = -1;
    public WindowState WindowState { get; set; } = WindowState.Normal;
    public string? LastComPort { get; set; } = "";
    public string LastOpenedFile { get; set; } = "";
    public string ScriptRepositoryFolder { get; set; } = "";
    public bool IsDevelopmentMode { get; set; } = false;
    public bool IsStepConfiguratorExpanded { get; set; } = false;
    public bool IsMeasSelectorExpanded { get; set; } = false;
    public bool IsStimSelectorExpanded { get; set; } = false;
    public bool IsExtStimSelectorExpanded { get; set; } = false;
    public bool IsScriptSelectorExpanded { get; set; } = false;
    public bool IsShellCommandEditorExpanded { get; set; } = false;

}