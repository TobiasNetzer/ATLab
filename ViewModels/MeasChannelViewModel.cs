using System.Collections.ObjectModel;
using ATLab.Interfaces;
using ATLab.Wrappers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class MeasChannelViewModel : ViewModelBase
{
    public ObservableCollection<string> MeasChannels { get; } = new();

    [ObservableProperty]
    private int _isSelectedH;
    
    [ObservableProperty]
    private int _isSelectedL;

    public MeasChannelViewModel(ITestHardware testHardware)
    {
        IsSelectedH = 0;
        IsSelectedL = 0;
        
        MeasChannels.Add("Off");

        for (int i = 1; i <= testHardware.HardwareInfo.MeasChannelCount; i++)
        {
            MeasChannels.Add(i.ToString());
        }
    }
}