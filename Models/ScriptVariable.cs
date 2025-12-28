using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class ScriptVariable : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _value = "1.0";

    public ScriptVariable Clone() => new() { Name = Name, Value = Value };
}