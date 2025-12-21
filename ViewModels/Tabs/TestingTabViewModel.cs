using System.Collections.ObjectModel;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels.Tabs;

public partial class TestingTabViewModel : ViewModelBase
{
    [ObservableProperty]
    private TestHardwareRelayChannelsViewModel _testHardwareRelayChannels;

    [ObservableProperty]
    private TestStepPresenterViewModel _testStepPresenter;
    
    [ObservableProperty]
    private string _title = "Testing";

    public TestingTabViewModel(IErrorService errorService, TestHardwareRelayChannelsViewModel testHardwareRelayChannels)
    {

        TestHardwareRelayChannels = testHardwareRelayChannels;

         TestStepPresenter = new TestStepPresenterViewModel(errorService, TestHardwareRelayChannels);
        
    }

    public TestingTabViewModel()
    {
        TestHardwareRelayChannels = new TestHardwareRelayChannelsViewModel(new DummyHardwareInfo());

        TestStepPresenter = new TestStepPresenterViewModel(new ErrorService(), TestHardwareRelayChannels);
        
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
                },
                new DummyHardwareInfo()
            )
        );
        
    }
}