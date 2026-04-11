using ATLab.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class PassFailAction : ObservableObject
{

    [ObservableProperty]
    private bool _isInvertResult;
    
    [ObservableProperty]
    private PassFailMode _mode = PassFailMode.CONTINUE;
    
    [ObservableProperty]
    private string _jumpToId = string.Empty;
    
    public PassFailAction()
    {
    }

    public PassFailAction(PassFailAction other)
    {
        IsInvertResult = other.IsInvertResult;
        Mode = other.Mode;
        JumpToId = other.JumpToId;
    }
}