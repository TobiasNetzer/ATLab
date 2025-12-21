using System.Collections.ObjectModel;
using System.Linq;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class StimChannelViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<RelayChannelViewModel> _stimChannels;

    [ObservableProperty]
    private ObservableCollection<CustomRelayChannelName> _customChannelNames;
    
    [ObservableProperty]
    private bool _isExpanded;

    public StimChannelViewModel(ObservableCollection<CustomRelayChannelName> customChannelNames)
    {
        StimChannels = new ObservableCollection<RelayChannelViewModel>();
        _customChannelNames = customChannelNames;
    }
    
    public StimChannelViewModel()
    {
        StimChannels = new ObservableCollection<RelayChannelViewModel>();

        _customChannelNames = new ObservableCollection<CustomRelayChannelName>();
        for (int i = 0; i < 16; i++)
        {
            _customChannelNames.Add(new CustomRelayChannelName("Channel", i));
        }
        var testStimState = new RelayGroup(16);
        LoadRelayStates(testStimState);
    }
    
    public void LoadRelayStates(RelayGroup relayGroup)
    {
        StimChannels = new ObservableCollection<RelayChannelViewModel>(
            relayGroup.Channels.Select(c => new RelayChannelViewModel(c, CustomChannelNames[c.ChannelIndex - 1])));
    }
}
