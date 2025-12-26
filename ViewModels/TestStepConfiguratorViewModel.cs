using System;
using System.Globalization;
using ATLab.Models;
using Avalonia.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class TestStepConfiguratorViewModel : ViewModelBase
{
    [ObservableProperty]
    private TestStepViewModel? _testStep;
    
    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private string? _testStepCustomUnit;
    
    public double Tolerance = 0.1;

    public void LoadTestStep(TestStepViewModel testStep)
    {
        if (TestStep?.TestStep != null)
        {
            TestStep.TestStep.PropertyChanged -= TestStepPropertyChanged;
        }

        TestStep = testStep;

        if (TestStep?.TestStep != null)
        {
            TestStep.TestStep.PropertyChanged += TestStepPropertyChanged;
        }
    }

    private void TestStepPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TestStep.TestStep.NominalValue))
        {
            UpdateLimitsFromNominal();
        }
    }

    private void UpdateLimitsFromNominal()
    {
        if (TestStep?.TestStep == null) return;
        
        TestStep.TestStep.UpperLimit = Math.Round(TestStep.TestStep.NominalValue * (1 + Tolerance), 4);
        TestStep.TestStep.LowerLimit = Math.Round(TestStep.TestStep.NominalValue * (1 - Tolerance), 4);
    }
}