using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.Json.Serialization;
using ATLab.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class TestStep : ObservableObject
{
    public TestStep()
    {
        _scriptVariables.CollectionChanged += ScriptVariables_CollectionChanged;
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
    private bool _repeatUntilPass;

    [ObservableProperty]
    [property: JsonPropertyOrder(10)]
    private string _comment = string.Empty;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(11)]
    private bool _showCommentOnTestStart;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(12)]
    private string _customMessageBoxImagePath = string.Empty;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(13)]
    private string _targetDevice = string.Empty;

    [ObservableProperty]
    [property: JsonPropertyOrder(14)]
    private string _scriptId = string.Empty;

    [ObservableProperty]
    [property: JsonPropertyOrder(15)]
    private ObservableCollection<ScriptVariable> _scriptVariables = new();
    
    [ObservableProperty]
    [property: JsonPropertyOrder(16)]
    private ShellCommand _shellCommand = new();

    [ObservableProperty]
    [property: JsonPropertyOrder(17)]
    private RelayMatrix _matrixState = new ();

    [ObservableProperty]
    [property: JsonIgnore]
    private RelayGroup _liveStimState = new(0);

    [ObservableProperty]
    [property: JsonIgnore]
    private RelayGroup _liveExtStimState = new(0);

    [JsonPropertyOrder(18)]
    public RelayGroupDto? StimState { get; set; }
    
    [JsonPropertyOrder(19)]
    public RelayGroupDto? ExtStimState { get; set; }

    partial void OnShellCommandChanged(ShellCommand? oldValue, ShellCommand newValue)
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
}
