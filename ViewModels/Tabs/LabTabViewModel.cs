using ATLab.DesignViewModels;
using ATLab.Interfaces;
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
    
    public LabTabViewModel()
    {
        StimChannelViewModel = new StimChannelDesignViewModel();
        ExtStimChannelViewModel = new  ExtStimChannelDesignViewModel();
    }
}