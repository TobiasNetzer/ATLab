using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class RelayChannelViewModel : ViewModelBase
{
    private readonly IChannelGroup? _channelGroup;
    
    [ObservableProperty]
    private int _channelIndex;

    [ObservableProperty]
    private string _channelName = "";

    [ObservableProperty]
    private bool _isEnabled;

    public RelayChannelViewModel(IChannelGroup? channelGroup, int channelIndex, string channelName)
    {
        _channelGroup = channelGroup;
        ChannelIndex = channelIndex + 1; // Index for UI not 0-based
        ChannelName = channelName;
        
        if (_channelGroup != null && channelIndex >= 0)
        {
            IsEnabled = _channelGroup[channelIndex];
        }
        else
        {
            IsEnabled = false;
        }
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (_channelGroup != null && ChannelIndex > 0)
        {
            _channelGroup[ChannelIndex - 1] = value; // Index here 0-based
            _channelGroup.CommitChanges();
        }
    }
}