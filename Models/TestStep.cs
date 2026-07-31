using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json.Serialization;
using ATLab.Enums;
using ATLab.Interfaces;
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
    public string Id { get; }

    [ObservableProperty]
    [property: JsonPropertyOrder(2)]
    private int _number = 1;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(3)]
    private string _name = string.Empty;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(4)]
    private string _nominalValueExpression = "0";

    [ObservableProperty]
    [property: JsonPropertyOrder(5)]
    private string _lowerLimitExpression = "0";

    [ObservableProperty]
    [property: JsonPropertyOrder(6)]
    private string _upperLimitExpression = "0";
    
    [ObservableProperty]
    [property: JsonIgnore]
    private double _nominalValue;
    
    [ObservableProperty]
    [property: JsonIgnore]
    private double _lowerLimit;
    
    [ObservableProperty]
    [property: JsonIgnore]
    private double _upperLimit;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(7)]
    private string _unit = string.Empty;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(8)]
    private string _delayExpression = "0";
    
    [ObservableProperty]
    [property: JsonIgnore]
    private int _delay;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(9)]
    private TestEvaluationSource _evaluationSource;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(10)]
    private bool _isCustomResponseMask;

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
    private bool _isAssignResultToVariable;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(17)]
    private string _variableName = string.Empty;

    [ObservableProperty]
    [property: JsonPropertyOrder(18)]
    private PassFailAction _onPass = new();
    
    [ObservableProperty]
    [property: JsonPropertyOrder(19)]
    private PassFailAction _onFail = new();
    
    [ObservableProperty]
    [property: JsonPropertyOrder(20)]
    private string _targetDeviceId = string.Empty;

    [ObservableProperty]
    [property: JsonPropertyOrder(21)]
    private string _scriptId = string.Empty;

    [ObservableProperty]
    [property: JsonPropertyOrder(22)]
    private ObservableCollection<CustomVariable> _scriptVariables = new();
    
    [ObservableProperty]
    [property: JsonPropertyOrder(23)]
    private ScriptCommand _command = new() { IsEvaluate = true }; // for single non-script commands, evaluate is always true
    
    [ObservableProperty]
    [property: JsonPropertyOrder(24)]
    private TestInterfaceConfig _interfaceConfig = new();
    
    [ObservableProperty]
    [property: JsonPropertyOrder(25)]
    private ShellCommand _shellCommand = new();
    
    [ObservableProperty]
    [property: JsonPropertyOrder(26)]
    private string _expression = string.Empty;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(27)]
    private string _filePath = string.Empty;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(28)]
    private ResponseMask  _responseMask = new();

    [ObservableProperty]
    [property: JsonPropertyOrder(29)]
    private RelayMatrix _matrixState = new ();

    [ObservableProperty]
    [property: JsonIgnore]
    private RelayGroup _liveStimState = new(0);

    [ObservableProperty]
    [property: JsonIgnore]
    private RelayGroup _liveExtStimState = new(0);

    [JsonPropertyOrder(30)]
    public RelayGroupDto? StimState { get; set; }
    
    [JsonPropertyOrder(31)]
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
    
    partial void OnInterfaceConfigChanged(TestInterfaceConfig? oldValue, TestInterfaceConfig newValue)
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

    partial void OnScriptVariablesChanged(ObservableCollection<CustomVariable>? oldValue, ObservableCollection<CustomVariable> newValue)
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
            foreach (CustomVariable item in e.OldItems) item.PropertyChanged -= Child_PropertyChanged;
        }

        if (e.NewItems != null)
        {
            foreach (CustomVariable item in e.NewItems) item.PropertyChanged += Child_PropertyChanged;
        }

        OnPropertyChanged(nameof(ScriptVariables));
    }

    private void Child_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (ReferenceEquals(sender, OnPass)) OnPropertyChanged(nameof(OnPass));
        else if (ReferenceEquals(sender, OnFail)) OnPropertyChanged(nameof(OnFail));
        else if (ReferenceEquals(sender, ShellCommand)) OnPropertyChanged(nameof(ShellCommand));
        else if (ReferenceEquals(sender, InterfaceConfig)) OnPropertyChanged(nameof(InterfaceConfig));
        else if (ReferenceEquals(sender, Command)) OnPropertyChanged(nameof(Command));
        else if (ReferenceEquals(sender, MatrixState)) OnPropertyChanged(nameof(MatrixState));
        else if (ReferenceEquals(sender, LiveStimState)) OnPropertyChanged(nameof(LiveStimState));
        else if (ReferenceEquals(sender, LiveExtStimState)) OnPropertyChanged(nameof(LiveExtStimState));
        else if (sender is CustomVariable) OnPropertyChanged(nameof(ScriptVariables));
    }

    public void UpdateDtos()
    {
        StimState = LiveStimState.ToDto();
        ExtStimState = LiveExtStimState.ToDto();
    }
    
    public void InitializeRuntimeState(IHardwareInfo hardwareInfo)
    {
        LiveStimState = new RelayGroup(hardwareInfo.StimChannelCount);
        LiveStimState.ApplyDto(StimState ?? new RelayGroupDto());

        LiveExtStimState = new RelayGroup(hardwareInfo.ExtStimChannelCount);
        LiveExtStimState.ApplyDto(ExtStimState ?? new RelayGroupDto());
    }
    
    public TestStep Clone(bool preserveId = false)
    {
        var clone = new TestStep(preserveId ? Id : Guid.NewGuid().ToString("N"))
        {
            Number = Number,
            Name = Name,
            NominalValueExpression = NominalValueExpression,
            NominalValue = NominalValue,
            LowerLimitExpression = LowerLimitExpression,
            LowerLimit = LowerLimit,
            UpperLimitExpression = UpperLimitExpression,
            UpperLimit = UpperLimit,
            Unit = Unit,
            DelayExpression = DelayExpression,
            Delay = Delay,
            EvaluationSource = EvaluationSource,
            IsCustomResponseMask = IsCustomResponseMask,
            Comment = Comment,
            IsShowComment = IsShowComment,
            CustomMessageBoxImagePath = CustomMessageBoxImagePath,
            IsIgnoreStep = IsIgnoreStep,
            IsExcludeFromExport = IsExcludeFromExport,
            IsAssignResultToVariable = IsAssignResultToVariable,
            VariableName = VariableName,
            OnPass = new PassFailAction(OnPass),
            OnFail = new PassFailAction(OnFail),
            TargetDeviceId = TargetDeviceId,
            ScriptId = ScriptId,
            Command = new ScriptCommand(Command),
            InterfaceConfig = new TestInterfaceConfig(InterfaceConfig),
            ShellCommand = new ShellCommand(ShellCommand),
            Expression = Expression,
            FilePath = FilePath,
            ResponseMask = new ResponseMask(ResponseMask),
            MatrixState = new RelayMatrix(MatrixState),
            StimState = StimState != null ? new RelayGroupDto(StimState) : null,
            ExtStimState = ExtStimState != null ? new RelayGroupDto(ExtStimState) : null
        };
        
        clone.ScriptVariables = new ObservableCollection<CustomVariable>(
            ScriptVariables.Select(v => v.Clone())
        );

        return clone;
    }

}