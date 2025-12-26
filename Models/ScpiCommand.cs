namespace ATLab.Models;

public sealed class ScpiCommand
{
    public string Command { get; set; } = string.Empty;
    public string? Expect { get; set; } // "none","string","double","int", etc.
}
