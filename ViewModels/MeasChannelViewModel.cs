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

    public void LoadActiveMeasChannels(int activeH, int activeL)
    {
        IsSelectedH = activeH;
        IsSelectedL = activeL;
    }
}