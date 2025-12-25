using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class TestConfigurationViewModel : ViewModelBase
{
    public readonly IHardwareInfo HardwareInfo;

    private ObservableCollection<CustomRelayChannelName> StimChannelNames { get; }
    private ObservableCollection<CustomRelayChannelName> ExtStimChannelNames { get; }
    private ObservableCollection<CustomRelayChannelName> MeasChannelNames { get; }

    [ObservableProperty]
    private StimChannelViewModel _stimChannelViewModel;
    
    [ObservableProperty]
    private ExtStimChannelViewModel _extStimChannelViewModel;
    
    public MeasChannelViewModel MeasChannelViewModel { get; }
    
    [ObservableProperty]
    private TestStepConfiguratorViewModel _testStepConfiguratorViewModel;
    
    public TestConfigurationViewModel(IHardwareInfo hardwareInfo, TestStepConfiguratorViewModel testStepConfiguratorViewModel)
    {
        HardwareInfo = hardwareInfo;
        TestStepConfiguratorViewModel = testStepConfiguratorViewModel;
        
        StimChannelNames = new ObservableCollection<CustomRelayChannelName>(
            Enumerable.Range(1, hardwareInfo.StimChannelCount).Select(i => new CustomRelayChannelName("", i)));
        
        ExtStimChannelNames = new ObservableCollection<CustomRelayChannelName>(
            Enumerable.Range(1, hardwareInfo.ExtStimChannelCount).Select(i => new CustomRelayChannelName("", i)));
        
        MeasChannelNames = new ObservableCollection<CustomRelayChannelName>(
            Enumerable.Range(1, hardwareInfo.MeasChannelCount).Select(i => new CustomRelayChannelName("", i)));
        
        StimChannelViewModel = new StimChannelViewModel(StimChannelNames);
        ExtStimChannelViewModel = new ExtStimChannelViewModel(ExtStimChannelNames);
        MeasChannelViewModel = new MeasChannelViewModel(MeasChannelNames);

        foreach (var c in StimChannelNames) c.PropertyChanged += (_, _) => OnChannelNameChanged();
        foreach (var c in ExtStimChannelNames) c.PropertyChanged += (_, _) => OnChannelNameChanged();
        foreach (var c in MeasChannelNames) c.PropertyChanged += (_, _) => OnChannelNameChanged();

        ResetToDefault();
    }

    public event Action? ConfigurationChanged;

    private void OnChannelNameChanged()
    {
        ConfigurationChanged?.Invoke();
    }
    
    public List<CustomRelayChannelName> GetStimNames() => StimChannelNames.ToList();
    public List<CustomRelayChannelName> GetExtStimNames() => ExtStimChannelNames.ToList();
    public List<CustomRelayChannelName> GetMeasNames() => MeasChannelNames.ToList();

    public void ApplyChannelNames(
        List<CustomRelayChannelName>? stim,
        List<CustomRelayChannelName>? extStim,
        List<CustomRelayChannelName>? meas)
    {
        ApplyList(StimChannelNames, stim);
        ApplyList(ExtStimChannelNames, extStim);
        ApplyList(MeasChannelNames, meas);
    }

    private void ApplyList(
        ObservableCollection<CustomRelayChannelName> target,
        List<CustomRelayChannelName>? source)
    {
        if (source == null) return;

        foreach (var item in source)
        {
            // ChannelIndex is 1-based
            int index = (item.ChannelIndex ?? 0) - 1;

            if (index >= 0 && index < target.Count)
                target[index].ChannelName = item.ChannelName;
        }
    }
    
    public void ResetToDefault()
    {
        foreach (var channel in StimChannelNames) channel.ChannelName = string.Empty;
        foreach (var channel in ExtStimChannelNames) channel.ChannelName = string.Empty;
        foreach (var channel in MeasChannelNames) channel.ChannelName = string.Empty;
    }
}