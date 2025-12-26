using System.Collections.ObjectModel;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class TestingTabViewModel : ViewModelBase
{
    [ObservableProperty]
    private TestHardwareRelayChannelsViewModel _testHardwareRelayChannels;
    
    [ObservableProperty]
    private TestStepConfiguratorViewModel _testStepConfiguratorViewModel;

    [ObservableProperty]
    private TestStepPresenterViewModel _testStepPresenter;

    public TestingTabViewModel(IErrorService errorService,
        TestHardwareRelayChannelsViewModel testHardwareRelayChannels,
        TestStepConfiguratorViewModel testStepConfiguratorViewModel,
        TestStepPresenterViewModel testStepPresenter)
    {
        TestHardwareRelayChannels = testHardwareRelayChannels;
        TestStepConfiguratorViewModel = testStepConfiguratorViewModel;
        TestStepPresenter = testStepPresenter;
        
        Title = "Testing";
    }

    public TestingTabViewModel()
    {
        var dummyHardwareInfo = new DummyHardwareInfo();
        TestStepConfiguratorViewModel = new TestStepConfiguratorViewModel();
        TestHardwareRelayChannels = new TestHardwareRelayChannelsViewModel(dummyHardwareInfo);
        TestStepPresenter = new TestStepPresenterViewModel(
            new ErrorService(), 
            TestHardwareRelayChannels, 
            new TestExecutor(new DummyTestStepRunner()), 
            TestStepConfiguratorViewModel,
            new FileDialogService(),
            new SettingsService(),
            new FileService(),
            new MessageBoxService());
        
        TestStepPresenter.TestSteps.Add(
            new TestStepViewModel(
                new TestStep
                {
                    Number = 1,
                    Name = "TestStep",
                    LowerLimit = 1,
                    UpperLimit = 1,
                    NominalValue = 1,
                },
                new DummyHardwareInfo()
            )
        );
        
    }
}