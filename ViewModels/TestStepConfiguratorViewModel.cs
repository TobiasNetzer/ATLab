using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ATLab.Enums;
using ATLab.Helpers;
using ATLab.Interfaces;
using ATLab.Models;
using Avalonia.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class TestStepConfiguratorViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    public List<TestEvaluationSource> EvaluationSources { get; } = Enum.GetValues<TestEvaluationSource>().ToList();
        
    [ObservableProperty]
    private TestStepViewModel? _testStepViewModel;
    
    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private string? _nominalValueText;

    [ObservableProperty]
    private string? _lowerLimitText;

    [ObservableProperty]
    private string? _upperLimitText;

    [ObservableProperty]
    private string? _delayText;
    
    public double Tolerance;

    public TestStepConfiguratorViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        IsExpanded = settingsService.Settings.IsStepConfiguratorExpanded;
    }

    public void LoadTestStep(TestStepViewModel testStep)
    {
        if (TestStepViewModel?.TestStep != null)
        {
            TestStepViewModel.TestStep.PropertyChanged -= TestStepPropertyChanged;
        }

        TestStepViewModel = testStep;

        if (TestStepViewModel?.TestStep != null)
        {
            TestStepViewModel.TestStep.PropertyChanged += TestStepPropertyChanged;
            UpdateStringProperties();
        }
    }

    private void UpdateStringProperties()
    {
        if (TestStepViewModel?.TestStep == null) return;

        NominalValueText = UnitParser.Format(TestStepViewModel.TestStep.NominalValue, TestStepViewModel.TestStep.Unit);
        LowerLimitText = UnitParser.Format(TestStepViewModel.TestStep.LowerLimit, TestStepViewModel.TestStep.Unit);
        UpperLimitText = UnitParser.Format(TestStepViewModel.TestStep.UpperLimit, TestStepViewModel.TestStep.Unit);
        DelayText = UnitParser.Format(TestStepViewModel.TestStep.Delay / 1000.0, "s");
        
        OnPropertyChanged(nameof(NominalValueText));
        OnPropertyChanged(nameof(LowerLimitText));
        OnPropertyChanged(nameof(UpperLimitText));
        OnPropertyChanged(nameof(DelayText));
    }

    private void TestStepPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (TestStepViewModel?.TestStep == null) return;

        switch (e.PropertyName)
        {
            case nameof(TestStepViewModel.TestStep.NominalValue):
                UpdateLimitsFromNominal();
                NominalValueText = UnitParser.Format(TestStepViewModel.TestStep.NominalValue, TestStepViewModel.TestStep.Unit);
                break;
            case nameof(TestStepViewModel.TestStep.LowerLimit):
                LowerLimitText = UnitParser.Format(TestStepViewModel.TestStep.LowerLimit, TestStepViewModel.TestStep.Unit);
                break;
            case nameof(TestStepViewModel.TestStep.UpperLimit):
                UpperLimitText = UnitParser.Format(TestStepViewModel.TestStep.UpperLimit, TestStepViewModel.TestStep.Unit);
                break;
            case nameof(TestStepViewModel.TestStep.Delay):
                DelayText = UnitParser.Format(TestStepViewModel.TestStep.Delay / 1000.0, "s");
                break;
            case nameof(TestStepViewModel.TestStep.Unit):
                UpdateStringProperties();
                break;
        }
    }

    partial void OnNominalValueTextChanged(string? value)
    {
        if (!UnitParser.TryParse(value, out var result, TestStepViewModel?.TestStep?.Unit)) return;
        if (TestStepViewModel?.TestStep != null)
            TestStepViewModel.TestStep.NominalValue = result;
    }

    partial void OnLowerLimitTextChanged(string? value)
    {
        if (TestStepViewModel?.TestStep != null)
        {
            if (UnitParser.TryParse(value, out var result, TestStepViewModel.TestStep.Unit))
            {
                // Ensure LowerLimit is always <= NominalValue
                var nominal = TestStepViewModel.TestStep.NominalValue;
                TestStepViewModel.TestStep.LowerLimit = Math.Min(result, nominal);
            }
            else
            {
                TestStepViewModel.TestStep.LowerLimit = 0;
            }

        }
    }

    partial void OnUpperLimitTextChanged(string? value)
    {
        if (TestStepViewModel?.TestStep != null)
        {
            if (UnitParser.TryParse(value, out var result, TestStepViewModel.TestStep.Unit))
            {
                // Ensure UpperLimit is always >= NominalValue
                var nominal = TestStepViewModel.TestStep.NominalValue;
                TestStepViewModel.TestStep.UpperLimit = Math.Max(result, nominal);
            }
            else
            {
                TestStepViewModel.TestStep.UpperLimit = 0;
            }
        }
    }

    partial void OnDelayTextChanged(string? value)
    {
        if (UnitParser.TryParse(value, out var result, "s"))
        {
            if (TestStepViewModel?.TestStep != null)
                TestStepViewModel.TestStep.Delay = (int)Math.Round(result * 1000);
        }
    }

    private void UpdateLimitsFromNominal()
    {
        if (TestStepViewModel?.TestStep == null) return;

        var nominal = TestStepViewModel.TestStep.NominalValue;
        var v1 = nominal * (1 + Tolerance);
        var v2 = nominal * (1 - Tolerance);

        // Ensure UpperLimit >= NominalValue and LowerLimit <= NominalValue
        TestStepViewModel.TestStep.UpperLimit = Math.Max(Math.Max(v1, v2), nominal);
        TestStepViewModel.TestStep.LowerLimit = Math.Min(Math.Min(v1, v2), nominal);
    }
    
    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsStepConfiguratorExpanded = value;
    }
}