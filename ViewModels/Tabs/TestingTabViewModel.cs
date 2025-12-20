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
}