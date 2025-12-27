using System.Collections.ObjectModel;
using System.Linq;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class ExtStimChannelViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<RelayChannelViewModel> _extStimChannels;
    
    private readonly ObservableCollection<CustomRelayChannelName> _customChannelNames;
    private readonly ISettingsService _settingsService;
    
    [ObservableProperty]
    private bool _isExpanded;

    public ExtStimChannelViewModel(
        ObservableCollection<CustomRelayChannelName> customChannelNames,
        ISettingsService settingsService)
    {
        ExtStimChannels = new ObservableCollection<RelayChannelViewModel>();
        _customChannelNames = customChannelNames;
        _settingsService = settingsService;
        IsExpanded = settingsService.Settings.IsExtStimSelectorExpanded;
    }
    
    public void LoadRelayStates(RelayGroup relayGroup)
    {
        ExtStimChannels = new ObservableCollection<RelayChannelViewModel>(
            relayGroup.Channels.Select(c => new RelayChannelViewModel(c, _customChannelNames[c.ChannelIndex - 1])));
    }

    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsExtStimSelectorExpanded = value;
    }
}