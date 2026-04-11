using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace ATLab.Models;

public partial class ResponseMask : ObservableObject
{
    [ObservableProperty]
    private string _mask = string.Empty;

    [ObservableProperty]
    private int _length;
    
    [ObservableProperty]
    private int _skip;

    [ObservableProperty]
    private bool _isOnlyNumeric;

    [ObservableProperty]
    [property: JsonIgnore]
    private string _lastOriginalResponse = string.Empty;

    [ObservableProperty]
    [property: JsonIgnore]
    private string _lastProcessedInput = string.Empty;

    [ObservableProperty]
    [property: JsonIgnore]
    private string _lastFinalResult = string.Empty;
    
    public ResponseMask()
    {
        
    }
    
    public ResponseMask(ResponseMask other)
    {
        Mask = other.Mask;
        Length = other.Length;
        Skip = other.Skip;
        IsOnlyNumeric = other.IsOnlyNumeric;
    }
}