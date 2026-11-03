namespace ATLab.Pcb.Controls;

/// <summary>
/// Colors of the probes and parts of a fixture, the same in the board view of the Fixture Designer and in its build
/// sheet, so a builder finds them by the colors known from the screen.
/// </summary>
public static class MarkerColors
{
    /// <summary>Probes; the Designer's board view gets it as its probe brush (PassBrush).</summary>
    public const string Probe = "#00C853";
    public const string LocatingPin = "#3B82F6";
    public const string PreCenteringPin = "#D946EF";
    public const string SupportPin = "#14B8A6";
    public const string PushRod = "#8B5CF6";
}