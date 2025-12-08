using System.Threading.Tasks;
using ATLab.DesignViewModels;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels.Tabs;

public partial class LabTabViewModel : ViewModelBase
{
    private readonly ITestHardware _testHardware;
    public StimChannelViewModel StimChannelViewModel { get; }
    public ExtStimChannelViewModel ExtStimChannelViewModel { get; }
    public MeasChannelViewModel MeasChannelViewModel { get; }

    [ObservableProperty]
    private string _title = "Lab";

    public LabTabViewModel(ITestHardware testHardware)
    {
        _testHardware = testHardware;
        StimChannelViewModel = new StimChannelViewModel(testHardware);
        ExtStimChannelViewModel = new  ExtStimChannelViewModel(testHardware);
        MeasChannelViewModel = new  MeasChannelViewModel(testHardware);
        
        
    }
    
    public LabTabViewModel()
    {
        StimChannelViewModel = new StimChannelDesignViewModel();
        ExtStimChannelViewModel = new ExtStimChannelDesignViewModel();
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