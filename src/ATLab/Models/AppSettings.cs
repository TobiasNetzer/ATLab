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

    /// <summary>The user's default layers of the report's board pictures, for projects without a choice.</summary>
    public ATLab.Pcb.Rendering.ReportLayerSettings? DefaultReportLayers { get; set; }
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
    public bool IsShowBoardView { get; set; }

    /// <summary>Share of the height of the step list area taken by the board view.</summary>
    public double BoardViewHeightRatio { get; set; } = 0.45;

    /// <summary>0 = top, 1 = bottom.</summary>
    public int BoardViewSide { get; set; }

    /// <summary>Whether the board view (and the board pictures of the PDF report) also highlight the zones of a net.</summary>
    public bool IsBoardHighlightZones { get; set; }

    /// <summary>Roles hidden in the board view (BoardRole names, e.g. "Stimulus").</summary>
    public System.Collections.Generic.List<string>? BoardHiddenRoles { get; set; }

    /// <summary>Colors the user chose for the roles of the board view: role name → "#RRGGBB".</summary>
    public System.Collections.Generic.Dictionary<string, string>? BoardRoleColors { get; set; }

    /// <summary>KiCad layers shown in the board view; null for the defaults.</summary>
    public System.Collections.Generic.List<string>? BoardVisibleLayers { get; set; }

    /// <summary>Colors ("#RRGGBB") the user chose for KiCad layers in the board view; the others keep their default.</summary>
    public System.Collections.Generic.Dictionary<string, string>? BoardLayerColors { get; set; }
}