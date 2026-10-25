using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>
/// Named value used in commands and expressions: a runtime variable, a script variable or a system variable.
/// </summary>
public partial class CustomVariable : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _value = string.Empty;

    // System variables use reserved names (e.g. "sys.Date") that bypass normalization
    public bool IsSystem { get; init; }

    partial void OnNameChanged(string value)
    {
        if (IsSystem)
            return;

        var normalized = value.Replace(" ", "_");
        
        normalized = Regex.Replace(normalized, @"[^A-Za-z0-9_]", "");
        
        if (normalized != value)
            Name = normalized;
    }

    public CustomVariable Clone() => new() { IsSystem = IsSystem, Name = Name, Value = Value };
}