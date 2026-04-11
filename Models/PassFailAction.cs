using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class PassFailAction : ObservableObject
{
    [ObservableProperty]
    private bool _isContinue = true;

    [ObservableProperty]
    private bool _isRepeat;
    
    [ObservableProperty]
    private bool _isEndTest;
    
    [ObservableProperty]
    private bool _isJumpTo;
    
    [ObservableProperty]
    private string _jumpToId = string.Empty;
    
    public PassFailAction()
    {
    }

    public PassFailAction(PassFailAction other)
    {
        IsContinue = other.IsContinue;
        IsRepeat = other.IsRepeat;
        IsEndTest = other.IsEndTest;
        IsJumpTo = other.IsJumpTo;
        JumpToId = other.JumpToId;
    }
}