using System.Collections.Generic;
using System.Collections.ObjectModel;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class MeasChannelViewModel : ViewModelBase
{
    
    private readonly ISettingsService _settingsService;
    
    public ObservableCollection<CustomRelayChannelName> CustomChannelNames { get; set; }

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private int _isSelectedH;
    
    [ObservableProperty]
    private int _isSelectedL;
    
    [ObservableProperty]
    private bool _isExternalProbeEnabled;
    
    private RelayMatrix? _relayMatrixState;
    
    public MeasChannelViewModel(
        ObservableCollection<CustomRelayChannelName> customChannelNames,
        ISettingsService settingsService)
    {
        CustomChannelNames = customChannelNames;
        IsSelectedH = 0;
        IsSelectedL = 0;
        _settingsService = settingsService;
        IsExpanded = settingsService.Settings.IsMeasSelectorExpanded;
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

    partial void OnIsExternalProbeEnabledChanged(bool value)
    {
        if (_relayMatrixState != null)
            _relayMatrixState.IsExternalProbe = value;
    }

    public void LoadActiveMeasChannels(RelayMatrix relayMatrixState)
    {
        _relayMatrixState = relayMatrixState;
        IsSelectedH = relayMatrixState.ActiveChannelHigh;
        IsSelectedL = relayMatrixState.ActiveChannelLow;
        IsExternalProbeEnabled = relayMatrixState.IsExternalProbe;
    }
    
    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsMeasSelectorExpanded = value;
    }
    
    [RelayCommand]
    private void ToggleChannels()
    {
        (IsSelectedH, IsSelectedL) = (IsSelectedL, IsSelectedH);
    }
}

public class MeasChannelOff : CustomRelayChannelName
{
    public MeasChannelOff() : base("Off", null) { }
}
