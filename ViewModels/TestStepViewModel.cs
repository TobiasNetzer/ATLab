using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;
public partial class TestStepViewModel : ViewModelBase
{
    public TestStep Model { get; }

    [ObservableProperty]
    private int _number;
    [ObservableProperty]
    private string? _name;
    [ObservableProperty]
    private double _value;
    [ObservableProperty]
    private double _lowerLimit;
    [ObservableProperty]
    private double _upperLimit;
    [ObservableProperty]
    private string? _result;
    [ObservableProperty]
    private string? _comment;
    
    [ObservableProperty]
    private RelayGroup _stimState;
    [ObservableProperty]
    private RelayGroup _extStimState;
    [ObservableProperty]
    private RelayMatrix _matrixState;

    public TestStepViewModel(TestStep model, IHardwareInfo hardwareInfo)
    {
        Model = model;
        
        StimState = new RelayGroup(hardwareInfo.StimChannelCount);
        ExtStimState = new RelayGroup(hardwareInfo.ExtStimChannelCount);
        MatrixState = new RelayMatrix(0,0);
        
        Number = model.Number;
        Name = model.Name;
        Value = model.Value;
        LowerLimit = model.LowerLimit;
        UpperLimit = model.UpperLimit;
        Result = model.Result;
        Comment = model.Comment;
        StimState.ApplyDto(Model.StimState ?? new RelayGroupDto());
        ExtStimState.ApplyDto(Model.ExtStimState ?? new RelayGroupDto());
        MatrixState = model.MatrixState ??  new RelayMatrix();
    }
    
    public void SyncBack()
    {
        Model.Number = Number;
        Model.Name = Name;
        Model.Value = Value;
        Model.LowerLimit = LowerLimit;
        Model.UpperLimit = UpperLimit;
        Model.Result = Result;
        Model.Comment = Comment;
        Model.StimState = StimState.ToDto();
        Model.ExtStimState = ExtStimState.ToDto();
        Model.MatrixState = MatrixState;
    }
}
