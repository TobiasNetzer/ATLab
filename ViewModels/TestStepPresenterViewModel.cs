using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class TestStepPresenterViewModel : ViewModelBase
{
    public ObservableCollection<TestStepViewModel> TestSteps { get; }

    [ObservableProperty]
    private TestHardwareRelayChannelsViewModel _testHardwareRelayChannels;
    
    public TestStepPresenterViewModel(TestHardwareRelayChannelsViewModel testHardwareRelayChannels)
    {
        TestSteps = new ObservableCollection<TestStepViewModel>();
        TestHardwareRelayChannels = testHardwareRelayChannels;
    }

    public void Load(IEnumerable<TestStep> steps)
    {
        TestSteps.Clear();
        foreach (var step in steps)
            TestSteps.Add(new TestStepViewModel(step));
    }

    public IEnumerable<TestStep> Save()
    {
        foreach (var vm in TestSteps)
            vm.SyncBack();
        return TestSteps.Select(vm => vm.Model).ToList();
    }
}
