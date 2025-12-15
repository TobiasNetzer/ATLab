using ATLab.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class TestStep : ViewModelBase
{
    [ObservableProperty]
    private int _number;
    public string Name { get; set; }
    public double Value { get; set; }
    public double LowerLimit { get; set; }
    public double UpperLimit { get; set; }
    public bool Result { get; set; }
}