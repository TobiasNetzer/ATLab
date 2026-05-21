using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;
using ATLab.Enums;

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
    private ResponseDisplayMode _responseDisplayMode = ResponseDisplayMode.ASCII;

    [ObservableProperty]
    [property: JsonIgnore]
    private string _originalResponse = string.Empty;

    [ObservableProperty]
    [property: JsonIgnore]
    private string _processedInput = string.Empty;

    [ObservableProperty]
    [property: JsonIgnore]
    private string _result = string.Empty;

    [ObservableProperty]
    [property: JsonIgnore]
    private byte[]? _rawOriginal;

    [ObservableProperty] 
    [property: JsonIgnore]
    private byte[]? _rawProcessed;
    
    public ResponseMask()
    {
        
    }
    
    public ResponseMask(ResponseMask other)
    {
        Mask = other.Mask;
        Length = other.Length;
        Skip = other.Skip;
        ResponseDisplayMode = other.ResponseDisplayMode;
    }
}