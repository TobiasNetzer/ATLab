using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class TestStepPresenterViewModel : ViewModelBase
{
    public ObservableCollection<TestStepViewModel> TestSteps { get; }

    [ObservableProperty]
    private TestHardwareRelayChannelsViewModel _testHardwareRelayChannels;
    
    [ObservableProperty]
    private TestStepViewModel? _selectedStep;
    
    [ObservableProperty]
    private int _selectedStepIndex;
    
    public TestStepPresenterViewModel(TestHardwareRelayChannelsViewModel testHardwareRelayChannels)
    {
        TestSteps = new ObservableCollection<TestStepViewModel>();
        TestHardwareRelayChannels = testHardwareRelayChannels;
    }
    
    partial void OnSelectedStepChanged(TestStepViewModel? value)
    {
        if (value != null)
        {
            TestHardwareRelayChannels.MeasChannelViewModel.LoadActiveMeasChannels(value.MatrixState);
            TestHardwareRelayChannels.StimChannelViewModel.LoadRelayStates(value.StimState);
            TestHardwareRelayChannels.ExtStimChannelViewModel.LoadRelayStates(value.ExtStimState);
        }
    }

    [RelayCommand]
    public void AddTestStep()
    {
        TestSteps.Add(new TestStepViewModel(new TestStep()));
    }
    
    [RelayCommand]
    public void RemoveTestStep()
    {
        TestSteps.RemoveAt(SelectedStepIndex);
    }
}
