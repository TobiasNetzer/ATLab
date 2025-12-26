namespace ATLab.Models;

public sealed class ScpiVariable
{
    public string Name { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = "1.0";
}