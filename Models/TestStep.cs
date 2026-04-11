using System;
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
        Id = Guid.NewGuid().ToString("N");
        HookEvents();
    }
    
    [JsonConstructor]
    public TestStep(string id)
    {
        Id = id;
        HookEvents();
    }
    
    [property: JsonPropertyOrder(1)]
    public string Id { get; init; }

    [ObservableProperty]
    [property: JsonPropertyOrder(2)]
    private int _number;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(3)]
    private string _name = string.Empty;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(4)]
    private double _nominalValue;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(5)]
    private double _lowerLimit;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(6)]
    private double _upperLimit;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(7)]
    private string _unit = string.Empty;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(8)]
    private int _delay;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(9)]
    private TestEvaluationSource _evaluationSource;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(10)]
    private bool _isCustomMask;

    [ObservableProperty]
    [property: JsonPropertyOrder(11)]
    private string _comment = string.Empty;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(12)]
    private bool _isShowComment;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(13)]
    private string _customMessageBoxImagePath = string.Empty;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(14)]
    private bool _isIgnoreStep;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(15)]
    private bool _isExcludeFromExport;

    [ObservableProperty]
    [property: JsonPropertyOrder(16)]
    private PassFailAction _onPass = new();
    
    [ObservableProperty]
    [property: JsonPropertyOrder(17)]
    private PassFailAction _onFail = new();
    
    [ObservableProperty]
    [property: JsonPropertyOrder(18)]
    private string _targetDevice = string.Empty;

    [ObservableProperty]
    [property: JsonPropertyOrder(19)]
    private string _scriptId = string.Empty;

    [ObservableProperty]
    [property: JsonPropertyOrder(20)]
    private ObservableCollection<ScriptVariable> _scriptVariables = new();
    
    [ObservableProperty]
    [property: JsonPropertyOrder(21)]
    private ScriptCommand _command = new() { IsEvaluate = true }; // for single non-script commands, evaluate is always true
    
    [ObservableProperty]
    [property: JsonPropertyOrder(22)]
    private ShellCommand _shellCommand = new();
    
    [ObservableProperty]
    [property: JsonPropertyOrder(23)]
    private ResponseMask  _responseMask = new();

    [ObservableProperty]
    [property: JsonPropertyOrder(24)]
    private RelayMatrix _matrixState = new ();

    [ObservableProperty]
    [property: JsonIgnore]
    private RelayGroup _liveStimState = new(0);

    [ObservableProperty]
    [property: JsonIgnore]
    private RelayGroup _liveExtStimState = new(0);

    [JsonPropertyOrder(25)]
    public RelayGroupDto? StimState { get; set; }
    
    [JsonPropertyOrder(26)]
    public RelayGroupDto? ExtStimState { get; set; }

    private void HookEvents()
    {
        ScriptVariables.CollectionChanged += ScriptVariables_CollectionChanged;
        MatrixState.PropertyChanged += Child_PropertyChanged;
    }

    partial void OnOnPassChanged(PassFailAction? oldValue, PassFailAction newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= Child_PropertyChanged;
        newValue.PropertyChanged += Child_PropertyChanged;
    }
    
    partial void OnOnFailChanged(PassFailAction? oldValue, PassFailAction newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= Child_PropertyChanged;
        newValue.PropertyChanged += Child_PropertyChanged;
    }

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
        OnPropertyChanged(nameof(OnPass));
        OnPropertyChanged(nameof(OnFail));
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
    
    public TestStep Clone(bool preserveId = false)
    {
        var clone = new TestStep(preserveId ? this.Id : Guid.NewGuid().ToString("N"))
        {
            Number = Number,
            Name = Name,
            NominalValue = NominalValue,
            LowerLimit = LowerLimit,
            UpperLimit = UpperLimit,
            Unit = Unit,
            Delay = Delay,
            EvaluationSource = EvaluationSource,
            IsCustomMask = IsCustomMask,
            Comment = Comment,
            IsShowComment = IsShowComment,
            CustomMessageBoxImagePath = CustomMessageBoxImagePath,
            IsIgnoreStep = IsIgnoreStep,
            IsExcludeFromExport = IsExcludeFromExport,
            OnPass = new PassFailAction(OnPass),
            OnFail = new PassFailAction(OnFail),
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