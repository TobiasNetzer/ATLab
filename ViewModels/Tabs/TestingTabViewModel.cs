using System.Collections.ObjectModel;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels.Tabs;

public partial class TestingTabViewModel : ViewModelBase
{
    private readonly IErrorService _errorService;
    private readonly ITestHardware _testHardware;
    
    public TestHardwareRelayChannelsViewModel TestHardwareRelayChannels { get; }
    
    public ObservableCollection<TestStep> TestSteps { get; set; }
    
    [ObservableProperty]
    private string _title = "Testing";

    public TestingTabViewModel(IErrorService errorService, ITestHardware testHardware, TestHardwareRelayChannelsViewModel testHardwareRelayChannels)
    {
        _errorService = errorService;
        _testHardware = testHardware;
        TestHardwareRelayChannels = testHardwareRelayChannels;

        TestSteps = new ObservableCollection<TestStep>();

        for (int i = 0; i < 100; i++)
        {
            TestSteps.Add(new TestStep()
            {
                Number = i,
                Result = i%2 == 0,
                Value = 2,
                LowerLimit = 1,
                UpperLimit = 5,
                Name =  $"Step {i}"
            });
        }
    }

    public TestingTabViewModel()
    {
        TestHardwareRelayChannels = new TestHardwareRelayChannelsViewModel();
        
        TestSteps = new ObservableCollection<TestStep>();
        
        for (int i = 0; i < 100; i++)
        {
            TestSteps.Add(new TestStep()
            {
                Number = i,
                Result = i%2 == 0,
                Value = 2,
                LowerLimit = 1,
                UpperLimit = 5,
                Name =  $"Step {i}"
            });
        }
    }
}