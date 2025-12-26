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
    
    public double Tolerance;

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

    private void UpdateStringProperties()
    {
        if (TestStepViewModel?.TestStep == null) return;

        NominalValueText = TestStepViewModel.TestStep.NominalValue.ToString(CultureInfo.CurrentCulture);
        LowerLimitText = TestStepViewModel.TestStep.LowerLimit.ToString(CultureInfo.CurrentCulture);
        UpperLimitText = TestStepViewModel.TestStep.UpperLimit.ToString(CultureInfo.CurrentCulture);
        DelayText = TestStepViewModel.TestStep.Delay.ToString(CultureInfo.CurrentCulture);
        
        OnPropertyChanged(nameof(NominalValueText));
        OnPropertyChanged(nameof(LowerLimitText));
        OnPropertyChanged(nameof(UpperLimitText));
        OnPropertyChanged(nameof(DelayText));
    }

    private void TestStepPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (TestStepViewModel?.TestStep == null) return;

        if (e.PropertyName == nameof(TestStepViewModel.TestStep.NominalValue))
        {
            UpdateLimitsFromNominal();
            NominalValueText = TestStepViewModel.TestStep.NominalValue.ToString(CultureInfo.CurrentCulture);
        }
        else if (e.PropertyName == nameof(TestStepViewModel.TestStep.LowerLimit))
        {
            LowerLimitText = TestStepViewModel.TestStep.LowerLimit.ToString(CultureInfo.CurrentCulture);
        }
        else if (e.PropertyName == nameof(TestStepViewModel.TestStep.UpperLimit))
        {
            UpperLimitText = TestStepViewModel.TestStep.UpperLimit.ToString(CultureInfo.CurrentCulture);
        }
        else if (e.PropertyName == nameof(TestStepViewModel.TestStep.Delay))
        {
            DelayText = TestStepViewModel.TestStep.Delay.ToString(CultureInfo.CurrentCulture);
        }
    }

        }
    }

    private void UpdateLimitsFromNominal()
    {
        if (TestStepViewModel?.TestStep == null) return;
        
        TestStepViewModel.TestStep.UpperLimit = Math.Round(TestStepViewModel.TestStep.NominalValue * (1 + Tolerance), 4);
        TestStepViewModel.TestStep.LowerLimit = Math.Round(TestStepViewModel.TestStep.NominalValue * (1 - Tolerance), 4);
    }
}