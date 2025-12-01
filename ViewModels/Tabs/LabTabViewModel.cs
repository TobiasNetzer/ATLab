using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class LabTabViewModel : ViewModelBase
{

    private ITestHardware _testHardware;

    public StimChannelViewModel StimChannelViewModel { get; }

    [ObservableProperty]
    private string title = "Lab";

    public LabTabViewModel(ITestHardware testHardware)
    {
        _testHardware = testHardware;
        StimChannelViewModel = new StimChannelViewModel(_testHardware);
    }
}