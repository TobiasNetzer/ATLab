using System.Collections.ObjectModel;
using ATLab.Interfaces;
using ATLab.Wrappers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class MeasChannelViewModel : ViewModelBase
{
    public ObservableCollection<RelayChannelViewModel> MeasHChannels { get; } = new();
    public ObservableCollection<RelayChannelViewModel> MeasLChannels { get; } = new();

    [ObservableProperty]
    private int _isSelectedH;
    
    [ObservableProperty]
    private int _isSelectedL;

    public MeasChannelViewModel(ITestHardware testHardware)
    {
        IsSelectedH = 0;
        IsSelectedL = 0;
        
        var measHGroup = new MeasHChannelGroup(testHardware);
        var measLGroup = new MeasLChannelGroup(testHardware);

        for (int i = 0; i < testHardware.HardwareInfo.MeasChannelCount; i++)
        {
            MeasHChannels.Add(new RelayChannelViewModel(measHGroup, i, $"MEAS"));
            MeasLChannels.Add(new RelayChannelViewModel(measLGroup, i, $"MEAS"));
        }
    }
}