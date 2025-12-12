using System.Collections.ObjectModel;
using ATLab.CTIA;
using ATLab.Interfaces;
using ATLab.Services;
using ATLab.Wrappers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class MeasChannelViewModel : ViewModelBase
{
    private readonly ITestHardware _testHardware;
    //public ObservableCollection<string> MeasChannels { get; }
    
    public ObservableCollection<RelayChannelViewModel> MeasChannels { get; }

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private int _isSelectedH;
    
    [ObservableProperty]
    private int _isSelectedL;

    public MeasChannelViewModel(ITestHardware testHardware)
    {
        _testHardware = testHardware;
        IsSelectedH = 0;
        IsSelectedL = 0;

        MeasChannels = new ObservableCollection<RelayChannelViewModel>();
        var measGroup = new MeasChannelGroup(testHardware);

        for (int i = 0; i < testHardware.HardwareInfo.MeasChannelCount; i++)
        {
            MeasChannels.Add(new RelayChannelViewModel(measGroup, i, ""));
        }
    }
    
    public MeasChannelViewModel()
    {
        IsSelectedH = 0;
        IsSelectedL = 0;

        MeasChannels = new ObservableCollection<RelayChannelViewModel>();
        var measGroup = new MeasChannelGroup(new CtiaHardware(new SimulationService()));

        for (int i = 0; i < 32; i++)
        {
            MeasChannels.Add(new RelayChannelViewModel(measGroup, i, ""));
        }
    }

    partial void OnIsSelectedHChanged(int value)
    {
        _testHardware.ActiveMeasChannelH = (byte)(value + 1); // index 1 based
    }

    partial void OnIsSelectedLChanged(int value)
    {
        _testHardware.ActiveMeasChannelL = (byte)(value + 1); // index 1 based
    }
}