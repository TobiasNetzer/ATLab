using System;
using System.Collections.Generic;
using System.Globalization;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using Avalonia.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class TestStepConfiguratorViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    public List<KeyValuePair<TestEvaluationSource, string>> EvaluationSources { get; } = new()
    {
        new(TestEvaluationSource.SCRIPT, "Internal Script"),
        new(TestEvaluationSource.COMMAND, "Console Script"),
    };
    
    [ObservableProperty]
    private TestStepViewModel? _testStepViewModel;
    
    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private string? _testStepCustomUnit;

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

    partial void OnNominalValueTextChanged(string? value)
    {
        if (!double.TryParse(value, CultureInfo.CurrentCulture, out var result)) return;
        if (TestStepViewModel?.TestStep != null)
            TestStepViewModel.TestStep.NominalValue = result;
    }

    partial void OnLowerLimitTextChanged(string? value)
    {
        if (double.TryParse(value, CultureInfo.CurrentCulture, out var result))
        {
            if (TestStepViewModel?.TestStep != null)
                TestStepViewModel.TestStep.LowerLimit = result;
        }
    }

    partial void OnUpperLimitTextChanged(string? value)
    {
        if (double.TryParse(value, CultureInfo.CurrentCulture, out var result))
        {
            if (TestStepViewModel?.TestStep != null)
                TestStepViewModel.TestStep.UpperLimit = result;
        }
    }

    partial void OnDelayTextChanged(string? value)
    {
        if (int.TryParse(value, CultureInfo.CurrentCulture, out var result))
        {
            if (TestStepViewModel?.TestStep != null)
                TestStepViewModel.TestStep.Delay = result;
        }
    }

    private void UpdateLimitsFromNominal()
    {
        if (TestStepViewModel?.TestStep == null) return;
        
        TestStepViewModel.TestStep.UpperLimit = Math.Round(TestStepViewModel.TestStep.NominalValue * (1 + Tolerance), 4);
        TestStepViewModel.TestStep.LowerLimit = Math.Round(TestStepViewModel.TestStep.NominalValue * (1 - Tolerance), 4);
    }
    
    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsStepConfiguratorExpanded = value;
    }
}