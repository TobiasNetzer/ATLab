using System.Collections.ObjectModel;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels.Tabs;

public partial class TestingTabViewModel : ViewModelBase
{
    private readonly IErrorService _errorService;
    private readonly ITestHardware _testHardware;

    [ObservableProperty]
    private TestHardwareRelayChannelsViewModel _testHardwareRelayChannels;

    [ObservableProperty]
    private TestStepPresenterViewModel _testStepPresenter;
    
    [ObservableProperty]
    private string _title = "Testing";

    public TestingTabViewModel(IErrorService errorService, ITestHardware testHardware, TestHardwareRelayChannelsViewModel testHardwareRelayChannels)
    {
        _errorService = errorService;
        _testHardware = testHardware;
        TestHardwareRelayChannels = testHardwareRelayChannels;

        TestStepPresenter = new TestStepPresenterViewModel(testHardwareRelayChannels);

        for (int i = 1; i <= 20; i++)
        {
            TestStepPresenter.TestSteps.Add(
                new TestStepViewModel(
                    new TestStep
                    {
                        Number = i,
                        Name = "Voltage In",
                        LowerLimit = 1,
                        UpperLimit = 1,
                        Value = 1,
                        Result = "Success",
                        Comment = "",
                        MatrixState = new RelayMatrix(0,0),
                        StimState = new RelayGroup(16),
                        ExtStimState = new RelayGroup(4)
                    }
                )
            );
        }
        
    }

    public TestingTabViewModel()
    {
        TestHardwareRelayChannels = new TestHardwareRelayChannelsViewModel(new DummyHardwareInfo());

        TestStepPresenter = new TestStepPresenterViewModel(TestHardwareRelayChannels);
        
        TestStepPresenter.TestSteps.Add(
            new TestStepViewModel(
                new TestStep
                {
                    Number = 1,
                    Name = "TestStep",
                    LowerLimit = 1,
                    UpperLimit = 1,
                    Value = 1,
                    Result = "Success"
                }
            )
        );
        
    }
    
    [ObservableProperty]
    private TestStepViewModel? _selectedStep;

    partial void OnSelectedStepChanged(TestStepViewModel? value)
    {
        if (value != null)
        {
            TestHardwareRelayChannels.StimChannelViewModel.LoadRelayStates(value.StimState);
        }
    }
}