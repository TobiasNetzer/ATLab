using ATLab.Models;
using Avalonia.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class TestStepConfiguratorViewModel : ViewModelBase
{
    private TestStepViewModel? _testStep;
    
    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private string? _testStepName;
    
    [ObservableProperty]
    private double _testStepLowerLimit;
    
    [ObservableProperty]
    private double _testStepNominalValue;
    
    [ObservableProperty]
    private double _testStepUpperLimit;
    
    [ObservableProperty]
    private string? _testStepCustomUnit;

    public void LoadTestStep(TestStepViewModel testStep)
    {
        _testStep = testStep;
        TestStepName = _testStep.Name;
        TestStepLowerLimit = _testStep.LowerLimit;
        TestStepNominalValue =  _testStep.NominalValue;
        TestStepUpperLimit = _testStep.UpperLimit;
    }

    partial void OnTestStepNameChanged(string? value)
    {
        if (_testStep != null)
            _testStep.Name = value;
    }

    partial void OnTestStepLowerLimitChanged(double value)
    {
        if (_testStep != null)
            _testStep.LowerLimit = value;
    }

    partial void OnTestStepNominalValueChanged(double value)
    {
        if (_testStep != null)
            _testStep.NominalValue = value;
    }
    
    partial void OnTestStepUpperLimitChanged(double value)
    {
        if (_testStep != null)
            _testStep.UpperLimit = value;
    }
}