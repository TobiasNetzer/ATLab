using ATLab.Models;
using Avalonia.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class TestStepConfiguratorViewModel : ViewModelBase
{
    private TestStepViewModel _testStep;
    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private string? _testStepName;

    public TestStepConfiguratorViewModel()
    {

    }

    public void LoadTestStep(TestStepViewModel testStep)
    {
        _testStep = testStep;
        TestStepName = _testStep.Name;
    }

    partial void OnTestStepNameChanged(string? value)
    {
        _testStep.Name = value;
    }
}