using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class ResponseMask : ObservableObject
{
    [ObservableProperty]
    private string _mask = string.Empty;

    [ObservableProperty]
    private int _length;
    
    [ObservableProperty]
    private int _skip;
    
    public ResponseMask()
    {
        
    }
    
    public ResponseMask(ResponseMask other)
    {
        Mask = other.Mask;
        Length = other.Length;
        Skip = other.Skip;
    }
}