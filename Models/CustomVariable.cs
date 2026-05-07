using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class CustomVariable : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _value = string.Empty;

    partial void OnNameChanged(string value)
    {
        var normalized = value.Replace(" ", "_");
        
        normalized = Regex.Replace(normalized, @"[^A-Za-z0-9_]", "");
        
        if (normalized != value)
            Name = normalized;
    }

    public CustomVariable Clone() => new() { Name = Name, Value = Value };
}