using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class LabTabViewModel : ViewModelBase
{
    public StimChannelViewModel StimChannelViewModel { get; }
    public ExtStimChannelViewModel ExtStimChannelViewModel { get; }

    [ObservableProperty]
    private string _title = "Lab";

    public LabTabViewModel(ITestHardware testHardware)
    {
        StimChannelViewModel = new StimChannelViewModel(testHardware);
        ExtStimChannelViewModel = new  ExtStimChannelViewModel(testHardware);
    }
}