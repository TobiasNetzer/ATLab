using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class ScpiVariable : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _defaultValue = "1.0";

    public ScpiVariable Clone() => new() { Name = Name, DefaultValue = DefaultValue };
}