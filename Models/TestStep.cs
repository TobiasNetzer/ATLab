using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json.Serialization;
using ATLab.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class TestStep : ObservableObject
{
    public TestStep()
    {
        _scriptVariables.CollectionChanged += ScriptVariables_CollectionChanged;
        _matrixState.PropertyChanged += Child_PropertyChanged;
    }

    [ObservableProperty]
    [property: JsonPropertyOrder(1)]
    private int _number;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(2)]
    private string _name = string.Empty;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(3)]
    private double _nominalValue;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(4)]
    private double _lowerLimit;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(5)]
    private double _upperLimit;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(6)]
    private string _unit = string.Empty;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(7)]
    private int _delay;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(8)]
    private TestEvaluationSource _evaluationSource;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(9)]
    private bool _customMask;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(10)]
    private bool _repeatUntilPass;

    [ObservableProperty]
    [property: JsonPropertyOrder(11)]
    private string _comment = string.Empty;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(12)]
    private bool _showCommentOnTestStart;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(13)]
    private string _customMessageBoxImagePath = string.Empty;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(14)]
    private bool _ignoreStep;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(15)]
    private bool _dontSaveResult;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(16)]
    private string _targetDevice = string.Empty;

    [ObservableProperty]
    [property: JsonPropertyOrder(17)]
    private string _scriptId = string.Empty;

    [ObservableProperty]
    [property: JsonPropertyOrder(18)]
    private ObservableCollection<ScriptVariable> _scriptVariables = new();
    
    [ObservableProperty]
    [property: JsonPropertyOrder(19)]
    private ScriptCommand _command = new();
    
    [ObservableProperty]
    [property: JsonPropertyOrder(20)]
    private ShellCommand _shellCommand = new();
    
    [ObservableProperty]
    [property: JsonPropertyOrder(21)]
    private ResponseMask  _responseMask = new();

    [ObservableProperty]
    [property: JsonPropertyOrder(22)]
    private RelayMatrix _matrixState = new ();

    [ObservableProperty]
    [property: JsonIgnore]
    private RelayGroup _liveStimState = new(0);

    [ObservableProperty]
    [property: JsonIgnore]
    private RelayGroup _liveExtStimState = new(0);

    [JsonPropertyOrder(23)]
    public RelayGroupDto? StimState { get; set; }
    
    [JsonPropertyOrder(24)]
    public RelayGroupDto? ExtStimState { get; set; }

    partial void OnCommandChanged(ScriptCommand? oldValue, ScriptCommand newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= Child_PropertyChanged;
        newValue.PropertyChanged += Child_PropertyChanged;
    }
    
    partial void OnShellCommandChanged(ShellCommand? oldValue, ShellCommand newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= Child_PropertyChanged;
        newValue.PropertyChanged += Child_PropertyChanged;
    }
    
    partial void OnResponseMaskChanged(ResponseMask? oldValue, ResponseMask newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= Child_PropertyChanged;
        newValue.PropertyChanged += Child_PropertyChanged;
    }

    partial void OnMatrixStateChanged(RelayMatrix? oldValue, RelayMatrix newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= Child_PropertyChanged;
        newValue.PropertyChanged += Child_PropertyChanged;
    }

    partial void OnLiveStimStateChanged(RelayGroup? oldValue, RelayGroup newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= Child_PropertyChanged;
        newValue.PropertyChanged += Child_PropertyChanged;
    }

    partial void OnLiveExtStimStateChanged(RelayGroup? oldValue, RelayGroup newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= Child_PropertyChanged;
        newValue.PropertyChanged += Child_PropertyChanged;
    }

    partial void OnScriptVariablesChanged(ObservableCollection<ScriptVariable>? oldValue, ObservableCollection<ScriptVariable> newValue)
    {
        if (oldValue != null)
        {
            oldValue.CollectionChanged -= ScriptVariables_CollectionChanged;
            foreach (var item in oldValue) item.PropertyChanged -= Child_PropertyChanged;
        }
        
        newValue.CollectionChanged += ScriptVariables_CollectionChanged;
        foreach (var item in newValue) item.PropertyChanged += Child_PropertyChanged;
        
    }

    private void ScriptVariables_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (ScriptVariable item in e.OldItems) item.PropertyChanged -= Child_PropertyChanged;
        }

        if (e.NewItems != null)
        {
            foreach (ScriptVariable item in e.NewItems) item.PropertyChanged += Child_PropertyChanged;
        }

        OnPropertyChanged(nameof(ScriptVariables));
    }

    private void Child_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ShellCommand));
        OnPropertyChanged(nameof(MatrixState));
        OnPropertyChanged(nameof(LiveStimState));
        OnPropertyChanged(nameof(LiveExtStimState));
        OnPropertyChanged(nameof(ScriptVariables));
    }

    public void UpdateDtos()
    {
        StimState = LiveStimState.ToDto();
        ExtStimState = LiveExtStimState.ToDto();
    }
    
    public TestStep Clone()
    {
        var clone = new TestStep
        {
            Number = Number,
            Name = Name,
            NominalValue = NominalValue,
            LowerLimit = LowerLimit,
            UpperLimit = UpperLimit,
            Unit = Unit,
            Delay = Delay,
            EvaluationSource = EvaluationSource,
            CustomMask = CustomMask,
            RepeatUntilPass = RepeatUntilPass,
            Comment = Comment,
            ShowCommentOnTestStart = ShowCommentOnTestStart,
            CustomMessageBoxImagePath = CustomMessageBoxImagePath,
            IgnoreStep = IgnoreStep,
            DontSaveResult = DontSaveResult,
            TargetDevice = TargetDevice,
            ScriptId = ScriptId,
            Command = new ScriptCommand(Command),
            ShellCommand = new ShellCommand(ShellCommand),
            ResponseMask = new ResponseMask(ResponseMask),
            MatrixState = new RelayMatrix(MatrixState),
            StimState = StimState != null ? new RelayGroupDto(StimState) : null,
            ExtStimState = ExtStimState != null ? new RelayGroupDto(ExtStimState) : null
        };
        
        clone.ScriptVariables = new ObservableCollection<ScriptVariable>(
            ScriptVariables.Select(v => v.Clone())
        );

        return clone;
    }

}
