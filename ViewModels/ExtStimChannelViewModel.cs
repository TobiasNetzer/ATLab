using System.Collections.ObjectModel;
using System.Linq;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class ExtStimChannelViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<RelayChannelViewModel> _extStimChannels;
    
    private readonly ObservableCollection<CustomRelayChannelName> _customChannelNames;
    
    [ObservableProperty]
    private bool _isExpanded = true;

    public ExtStimChannelViewModel(ObservableCollection<CustomRelayChannelName> customChannelNames)
    {
        ExtStimChannels = new ObservableCollection<RelayChannelViewModel>();
        _customChannelNames = customChannelNames;
    }

    public ExtStimChannelViewModel()
    {
        ExtStimChannels = new ObservableCollection<RelayChannelViewModel>();

        _customChannelNames = new ObservableCollection<CustomRelayChannelName>();
        for (int i = 0; i < 4; i++)
        {
            _customChannelNames.Add(new CustomRelayChannelName("Channel", i));
        }
        var testStimState = new RelayGroup(4);
        LoadRelayStates(testStimState);
    }
    
    public void LoadRelayStates(RelayGroup relayGroup)
    {
        ExtStimChannels = new ObservableCollection<RelayChannelViewModel>(
            relayGroup.Channels.Select(c => new RelayChannelViewModel(c, _customChannelNames[c.ChannelIndex - 1])));
    }
}