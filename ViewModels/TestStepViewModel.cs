using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;
public partial class TestStepViewModel : ViewModelBase
{
    private readonly TestStep _model;

    [ObservableProperty]
    private int _number;
    [ObservableProperty]
    private string? _name;
    [ObservableProperty]
    private double _nominalValue;
    [ObservableProperty]
    private double _lowerLimit;
    [ObservableProperty]
    private double _upperLimit;
    [ObservableProperty]
    private int _delay;
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
        _model = model;
        
        StimState = new RelayGroup(hardwareInfo.StimChannelCount);
        ExtStimState = new RelayGroup(hardwareInfo.ExtStimChannelCount);
        MatrixState = new RelayMatrix(0,0);
        
        Number = model.Number;
        Name = model.Name;
        NominalValue = model.NominalValue;
        LowerLimit = model.LowerLimit;
        UpperLimit = model.UpperLimit;
        Result = model.Result;
        Delay = model.Delay;
        Comment = model.Comment;
        StimState.ApplyDto(_model.StimState ?? new RelayGroupDto());
        ExtStimState.ApplyDto(_model.ExtStimState ?? new RelayGroupDto());
        MatrixState = model.MatrixState ??  new RelayMatrix();
    }
    
    public void SyncBack()
    {
        _model.Number = Number;
        _model.Name = Name;
        _model.NominalValue = NominalValue;
        _model.LowerLimit = LowerLimit;
        _model.UpperLimit = UpperLimit;
        _model.Delay = Delay;
        _model.Result = Result;
        _model.Comment = Comment;
        _model.StimState = StimState.ToDto();
        _model.ExtStimState = ExtStimState.ToDto();
        _model.MatrixState = MatrixState;
    }
    
    public TestStep GetModel() 
    {
        SyncBack();
        return _model;
    }
}
