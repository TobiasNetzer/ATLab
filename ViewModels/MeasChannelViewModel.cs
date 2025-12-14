using System.Collections.Generic;
using System.Collections.ObjectModel;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class MeasChannelViewModel : ViewModelBase
{
    public ObservableCollection<CustomRelayChannelName> CustomChannelNames { get; set; }

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private int _isSelectedH;
    
    [ObservableProperty]
    private int _isSelectedL;
    
    private RelayMatrix? _relayMatrixState;
    
    public MeasChannelViewModel(ObservableCollection<CustomRelayChannelName> customChannelNames)
    {
        CustomChannelNames = customChannelNames;
        IsSelectedH = 0;
        IsSelectedL = 0;
    }
    
    public MeasChannelViewModel()
    {
        IsSelectedH = 0;
        IsSelectedL = 0;

        CustomChannelNames = new ObservableCollection<CustomRelayChannelName>();
        for (int i = 0; i < 32; i++)
        {
            CustomChannelNames.Add(new CustomRelayChannelName("Channel", i));
        }
    }
    
    public IEnumerable<CustomRelayChannelName> CustomChannelNamesWithOffMember
    {
        get
        {
            yield return new MeasChannelOff();
            foreach (var channel in CustomChannelNames)
                yield return channel;
        }
    }

    partial void OnIsSelectedHChanged(int value)
    {
        if (_relayMatrixState != null)
            _relayMatrixState.ActiveChannelHigh = value;
    }

    partial void OnIsSelectedLChanged(int value)
    {
        if (_relayMatrixState != null)
            _relayMatrixState.ActiveChannelLow = value;
    }

    public void LoadActiveMeasChannels(RelayMatrix relayMatrixState)
    {
        _relayMatrixState = relayMatrixState;
        IsSelectedH = relayMatrixState.ActiveChannelHigh;
        IsSelectedL = relayMatrixState.ActiveChannelLow;
    }
}

public class MeasChannelOff : CustomRelayChannelName
{
    public MeasChannelOff() : base("Off", null) { }
}
