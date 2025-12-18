using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;
public partial class TestStepViewModel : ViewModelBase
{
    public TestStep Model { get; }

    [ObservableProperty]
    private int _number;
    [ObservableProperty]
    private string _name;
    [ObservableProperty]
    private double _value;
    [ObservableProperty]
    private double _lowerLimit;
    [ObservableProperty]
    private double _upperLimit;
    [ObservableProperty]
    private bool _result;
    
    [ObservableProperty]
    private RelayGroup _stimState;
    [ObservableProperty]
    private RelayGroup _extStimState;
    [ObservableProperty]
    private RelayMatrix _matrixState;

    public TestStepViewModel(TestStep model)
    {
        Model = model;
        
        Number = model.Number;
        Name = model.Name;
        Value = model.Value;
        LowerLimit = model.LowerLimit;
        UpperLimit = model.UpperLimit;
        Result = model.Result;
        StimState = model.StimState;
        ExtStimState = model.ExtStimState;
        MatrixState = model.MatrixState;
    }
    
    public void SyncBack()
    {
        Model.Number = Number;
        Model.Name = Name;
        Model.Value = Value;
        Model.LowerLimit = LowerLimit;
        Model.UpperLimit = UpperLimit;
        Model.Result = Result;
        Model.StimState = StimState;
        Model.ExtStimState = ExtStimState;
        Model.MatrixState = MatrixState;
    }
}
