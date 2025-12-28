namespace ATLab.Models;

public sealed class ScriptCommand
{
    public string Command { get; set; } = string.Empty;
    public bool ExpectResponse { get; set; }
    public int DelayMs { get; set; }
    public int TimeoutMs { get; set; } = 1000;
}
