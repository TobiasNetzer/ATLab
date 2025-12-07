using System.Collections.ObjectModel;
using ATLab.Interfaces;
using ATLab.Wrappers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class MeasChannelViewModel : ViewModelBase
{
    ITestHardware _testHardware;
    public ObservableCollection<string> MeasChannels { get; } = new();

    [ObservableProperty]
    private byte _isSelectedH;
    
    [ObservableProperty]
    private byte _isSelectedL;

    public MeasChannelViewModel(ITestHardware testHardware)
    {
        _testHardware = testHardware;
        IsSelectedH = 0;
        IsSelectedL = 0;
        
        MeasChannels.Add("Off");

        for (int i = 1; i <= testHardware.HardwareInfo.MeasChannelCount; i++)
        {
            MeasChannels.Add(i.ToString());
        }
    }

    partial void OnIsSelectedHChanged(byte value)
    {
        _testHardware.ActiveMeasChannelH = value;
    }

    partial void OnIsSelectedLChanged(byte value)
    {
        _testHardware.ActiveMeasChannelL = value;
    }
}