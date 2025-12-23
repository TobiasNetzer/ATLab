using System;
using System.Globalization;
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
    private string? _testStepLowerLimit;
    
    [ObservableProperty]
    private string? _testStepNominalValue;
    
    [ObservableProperty]
    private string? _testStepUpperLimit;
    
    [ObservableProperty]
    private string? _testStepCustomUnit;
    
    private bool _suppressCallback = false;

    public void LoadTestStep(TestStepViewModel testStep)
    {
        _suppressCallback = true;
        
        _testStep = testStep;
        TestStepName = _testStep.Name;
        TestStepLowerLimit = _testStep.LowerLimit.ToString(CultureInfo.CurrentCulture);
        TestStepNominalValue =  _testStep.NominalValue.ToString(CultureInfo.CurrentCulture);
        TestStepUpperLimit = _testStep.UpperLimit.ToString(CultureInfo.CurrentCulture);
        
        _suppressCallback = false;
    }

    partial void OnTestStepNameChanged(string? value)
    {
        if (_testStep != null)
            _testStep.Name = value;
    }

    partial void OnTestStepLowerLimitChanged(string? value)
    {
        if (_testStep != null)
            _testStep.LowerLimit = double.TryParse(value, out var lowerLimit) ? lowerLimit : 0;
    }

    partial void OnTestStepNominalValueChanged(string? value)
    {
        if(_suppressCallback)
            return;
        
        if (_testStep != null) 
        {
            _testStep.NominalValue = double.TryParse(value, out var nominalValue) ? nominalValue : 0;
            var upperLimit = Math.Round(_testStep.NominalValue * 1.05, 4);
            var lowerLimit = Math.Round(_testStep.NominalValue * 0.95, 4);
            
            TestStepLowerLimit = lowerLimit.ToString(CultureInfo.CurrentCulture);
            TestStepUpperLimit = upperLimit.ToString(CultureInfo.CurrentCulture);
            _testStepLowerLimit = lowerLimit.ToString(CultureInfo.CurrentCulture);
            _testStepUpperLimit = upperLimit.ToString(CultureInfo.CurrentCulture);
        }
        
    }
    
    partial void OnTestStepUpperLimitChanged(string? value)
    {
        if (_testStep != null)
            _testStep.UpperLimit = double.TryParse(value, out var upperLimit) ? upperLimit : 0;
    }
}