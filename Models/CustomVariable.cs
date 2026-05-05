using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class CustomVariable : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _value = string.Empty;

    public CustomVariable Clone() => new() { Name = Name, Value = Value };
}