using System.Threading.Tasks;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels.Tabs;

public partial class LabTabViewModel : ViewModelBase
{
    private readonly ITestHardware _testHardware;
    
    public TestHardwareRelayChannelsViewModel TestHardwareRelayChannels { get; }

    [ObservableProperty]
    private string _title = "Lab";

    public LabTabViewModel(ITestHardware testHardware, TestHardwareRelayChannelsViewModel testHardwareRelayChannels)
    {
        _testHardware = testHardware;
        TestHardwareRelayChannels = testHardwareRelayChannels;
    }
    
    public LabTabViewModel()
    {
        TestHardwareRelayChannels = new TestHardwareRelayChannelsViewModel();
    }
    
    [ObservableProperty]
    private bool _isBusy;

    private bool CanUpdateRelayStates() => !App.SimulationMode && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanUpdateRelayStates))]
    private async Task UpdateTestHardwareRelayStates()
    {
        try
        {
            IsBusy = true;
            var result = await _testHardware.UpdateRelayStates();

            if (!result.IsSuccess)
            {
                // show error
            }
        }
        catch
        {
            //
        }
        finally
        {
            IsBusy = false;
        }
    }
}