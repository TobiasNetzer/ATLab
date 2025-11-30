using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class LabTabViewModel : ViewModelBase
{

    private IUniversalTestHardwareInterface _testHardware;

    public DeviceViewModel DeviceViewModel { get; }

    [ObservableProperty]
    private string title = "Lab";

    public LabTabViewModel(IUniversalTestHardwareInterface testHardware)
    {
        _testHardware = testHardware;
        DeviceViewModel = new DeviceViewModel(_testHardware);
    }
}