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
    
    [ObservableProperty]
    private string _title = "Testing";

    public TestingTabViewModel(IErrorService errorService, TestConfigurationViewModel testConfiguration)
    {

        TestConfiguration = testConfiguration;

         TestStepPresenter = new TestStepPresenterViewModel(errorService, TestConfiguration, new TestExecutor(new DummyTestStepRunner()));
        
    }

    public TestingTabViewModel()
    {
        TestConfiguration = new TestConfigurationViewModel(new DummyHardwareInfo());

        TestStepPresenter = new TestStepPresenterViewModel(new ErrorService(), TestConfiguration, new TestExecutor(new DummyTestStepRunner()));
        
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