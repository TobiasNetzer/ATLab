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
    
    public TestConfigurationViewModel(IHardwareInfo hardwareInfo)
    {
        HardwareInfo = hardwareInfo;
        
        StimChannelNames = new ObservableCollection<CustomRelayChannelName>();
        for (int i = 0; i < hardwareInfo.StimChannelCount; i++)
        {
            StimChannelNames.Add(new CustomRelayChannelName("", i+1));
        }
        
        StimChannelViewModel = new StimChannelViewModel(StimChannelNames);
        
        ExtStimChannelNames = new ObservableCollection<CustomRelayChannelName>();
        for (int i = 0; i < hardwareInfo.ExtStimChannelCount; i++)
        {
            ExtStimChannelNames.Add(new CustomRelayChannelName("", i+1));
        }
        ExtStimChannelViewModel = new ExtStimChannelViewModel(ExtStimChannelNames);
        
        MeasChannelNames = new ObservableCollection<CustomRelayChannelName>();
        for (int i = 0; i < hardwareInfo.MeasChannelCount; i++)
        {
            MeasChannelNames.Add(new CustomRelayChannelName("", i+1));
        }
        MeasChannelViewModel = new MeasChannelViewModel(MeasChannelNames);
        
        TestStepConfiguratorViewModel =  new TestStepConfiguratorViewModel();
    }
    
    public List<CustomRelayChannelName> GetStimNames() =>
        StimChannelNames.ToList();

    public List<CustomRelayChannelName> GetExtStimNames() =>
        ExtStimChannelNames.ToList();

    public List<CustomRelayChannelName> GetMeasNames() =>
        MeasChannelNames.ToList();

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
        if (source == null)
            return;

        foreach (var item in source)
        {
            // ChannelIndex is 1-based
            int index = (item.ChannelIndex ?? 0) - 1;

            if (index >= 0 && index < target.Count)
                target[index].ChannelName = item.ChannelName;
        }
    }

}