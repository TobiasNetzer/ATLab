using System;
using System.Collections.ObjectModel;
using ATLab.Interfaces;
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
    
    private readonly IErrorService _errorService;
    
    public TestStepPresenterViewModel(IErrorService errorService, TestHardwareRelayChannelsViewModel testHardwareRelayChannels)
    {
        _errorService = errorService;
        TestSteps = new ObservableCollection<TestStepViewModel>();
        TestHardwareRelayChannels = testHardwareRelayChannels;
    }
    
    partial void OnSelectedStepChanged(TestStepViewModel? value)
    {
        if (value != null)
        {
            try
            {
                TestHardwareRelayChannels.MeasChannelViewModel.LoadActiveMeasChannels(value.MatrixState);
                TestHardwareRelayChannels.StimChannelViewModel.LoadRelayStates(value.StimState);
                TestHardwareRelayChannels.ExtStimChannelViewModel.LoadRelayStates(value.ExtStimState);
            }
            catch (Exception ex)
            {
                _errorService.AddError("Exception: " + ex.Message);
            }
            
        }
    }

    private void RenumberTestSteps()
    {
        for (int i = 0; i < TestSteps.Count; i++)
        {
            TestSteps[i].Number = i + 1; // 1‑based numbering
        }
    }

    [RelayCommand]
    private void AddTestStep()
    {
        TestSteps.Add(new TestStepViewModel(new TestStep(), TestHardwareRelayChannels.HardwareInfo));
        RenumberTestSteps();
    }
    
    [RelayCommand]
    private void RemoveTestStep()
    {
        TestSteps.RemoveAt(SelectedStepIndex);
        RenumberTestSteps();
    }
}
