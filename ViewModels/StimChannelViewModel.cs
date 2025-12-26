using System.Collections.ObjectModel;
using System.Linq;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class StimChannelViewModel : ViewModelBase
{
    
    private readonly ISettingsService _settingsService;
    
    [ObservableProperty]
    private ObservableCollection<RelayChannelViewModel> _stimChannels;

    [ObservableProperty]
    private ObservableCollection<CustomRelayChannelName> _customChannelNames;
    
    [ObservableProperty]
    private bool _isExpanded;

    public StimChannelViewModel(
        ObservableCollection<CustomRelayChannelName> customChannelNames,
        ISettingsService settingsService)
    {
        StimChannels = new ObservableCollection<RelayChannelViewModel>();
        _customChannelNames = customChannelNames;
        _settingsService = settingsService;
        IsExpanded = settingsService.Settings.IsStimSelectorExpanded;
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
    
    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsStimSelectorExpanded = value;
    }
}
