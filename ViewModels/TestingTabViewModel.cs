using System.Collections.ObjectModel;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class TestingTabViewModel : ViewModelBase
{
    [ObservableProperty]
    private TestConfigurationViewModel _testConfiguration;

    [ObservableProperty]
    private TestStepPresenterViewModel _testStepPresenter;

    public TestingTabViewModel(IErrorService errorService, TestConfigurationViewModel testConfiguration, TestStepPresenterViewModel testStepPresenter)
    {
        TestConfiguration = testConfiguration;
        TestStepPresenter = testStepPresenter;
        
        Title = "Testing";
    }

    public TestingTabViewModel()
    {
        var dummyHardwareInfo = new DummyHardwareInfo();
        var configurator = new TestStepConfiguratorViewModel();
        TestConfiguration = new TestConfigurationViewModel(dummyHardwareInfo, configurator);
        TestStepPresenter = new TestStepPresenterViewModel(
            new ErrorService(), 
            TestConfiguration, 
            new TestExecutor(new DummyTestStepRunner()), 
            configurator,
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
                    Result = "Success"
                },
                new DummyHardwareInfo()
            )
        );
        
    }
}