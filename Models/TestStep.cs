using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class TestStep : ObservableObject
{
    [ObservableProperty]
    [property: JsonPropertyOrder(1)]
    private int _number;
    
    [ObservableProperty]
    [property: JsonPropertyOrder(2)]
    private string? _name;
    
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
    private int _delay;

    [ObservableProperty]
    [property: JsonPropertyOrder(7)]
    private string? _comment;

    [ObservableProperty]
    [property: JsonPropertyOrder(8)]
    private RelayMatrix? _matrixState;

    [ObservableProperty]
    [JsonIgnore]
    private RelayGroup? _liveStimState;

    [ObservableProperty]
    [JsonIgnore]
    private RelayGroup? _liveExtStimState;

    [JsonPropertyOrder(9)]
    public RelayGroupDto? StimState { get; set; }
    
    [JsonPropertyOrder(10)]
    public RelayGroupDto? ExtStimState { get; set; }

    partial void OnMatrixStateChanged(RelayMatrix? oldValue, RelayMatrix? newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= Child_PropertyChanged;
        if (newValue != null) newValue.PropertyChanged += Child_PropertyChanged;
    }

    partial void OnLiveStimStateChanged(RelayGroup? oldValue, RelayGroup? newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= Child_PropertyChanged;
        if (newValue != null) newValue.PropertyChanged += Child_PropertyChanged;
    }

    partial void OnLiveExtStimStateChanged(RelayGroup? oldValue, RelayGroup? newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= Child_PropertyChanged;
        if (newValue != null) newValue.PropertyChanged += Child_PropertyChanged;
    }

    private void Child_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(MatrixState));
        OnPropertyChanged(nameof(LiveStimState));
        OnPropertyChanged(nameof(LiveExtStimState));
    }

    public void UpdateDtos()
    {
        StimState = LiveStimState?.ToDto();
        ExtStimState = LiveExtStimState?.ToDto();
    }
}