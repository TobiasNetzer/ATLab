using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Helpers;
using ATLab.Interfaces;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class TestStepConfiguratorViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IFileDialogService _fileDialogService;

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

    public TestStepConfiguratorViewModel(
        ISettingsService settingsService,
        IFileDialogService fileDialogService)
    {
        _settingsService = settingsService;
        _fileDialogService = fileDialogService;
        
        IsExpanded = settingsService.Settings.IsStepConfiguratorExpanded;
    }

    public void LoadTestStep(TestStepViewModel testStep)
    {
        if (TestStepViewModel?.TestStep != null)
        {
            TestStepViewModel.TestStep.PropertyChanged -= TestStepPropertyChanged;
        }

        TestStepViewModel = testStep;

        if (TestStepViewModel?.TestStep == null) return;
        
        TestStepViewModel.TestStep.PropertyChanged += TestStepPropertyChanged;
        UpdateStringProperties();
    }

    private void UpdateStringProperties()
    {
        if (TestStepViewModel?.TestStep == null) return;

        NominalValueText = string.IsNullOrEmpty(TestStepViewModel.TestStep.Unit) ? TestStepViewModel.TestStep.NominalValue.ToString(CultureInfo.CurrentCulture) : UnitParser.Format(TestStepViewModel.TestStep.NominalValue, TestStepViewModel.TestStep.Unit);
        LowerLimitText = string.IsNullOrEmpty(TestStepViewModel.TestStep.Unit) ? TestStepViewModel.TestStep.LowerLimit.ToString(CultureInfo.CurrentCulture) : UnitParser.Format(TestStepViewModel.TestStep.LowerLimit, TestStepViewModel.TestStep.Unit);
        UpperLimitText = string.IsNullOrEmpty(TestStepViewModel.TestStep.Unit) ? TestStepViewModel.TestStep.UpperLimit.ToString(CultureInfo.CurrentCulture) : UnitParser.Format(TestStepViewModel.TestStep.UpperLimit, TestStepViewModel.TestStep.Unit);
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
                NominalValueText = string.IsNullOrEmpty(TestStepViewModel.TestStep.Unit) ? TestStepViewModel.TestStep.NominalValue.ToString(CultureInfo.CurrentCulture) : UnitParser.Format(TestStepViewModel.TestStep.NominalValue, TestStepViewModel.TestStep.Unit);
                break;
            case nameof(TestStepViewModel.TestStep.LowerLimit):
                LowerLimitText = string.IsNullOrEmpty(TestStepViewModel.TestStep.Unit) ? TestStepViewModel.TestStep.LowerLimit.ToString(CultureInfo.CurrentCulture) : UnitParser.Format(TestStepViewModel.TestStep.LowerLimit, TestStepViewModel.TestStep.Unit);
                break;
            case nameof(TestStepViewModel.TestStep.UpperLimit):
                UpperLimitText = string.IsNullOrEmpty(TestStepViewModel.TestStep.Unit) ? TestStepViewModel.TestStep.UpperLimit.ToString(CultureInfo.CurrentCulture) : UnitParser.Format(TestStepViewModel.TestStep.UpperLimit, TestStepViewModel.TestStep.Unit);
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
        if (TestStepViewModel?.TestStep == null) return;
        if (UnitParser.TryParse(value, out var result, TestStepViewModel.TestStep.Unit))
        {
            TestStepViewModel.TestStep.NominalValue = result;
        }
        else
        {
            TestStepViewModel.TestStep.NominalValue = 0;
        }
            
        Dispatcher.UIThread.Post(UpdateStringProperties);
    }

    partial void OnLowerLimitTextChanged(string? value)
    {
        if (TestStepViewModel?.TestStep == null) return;
        
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
        
        Dispatcher.UIThread.Post(UpdateStringProperties);
    }

    partial void OnUpperLimitTextChanged(string? value)
    {
        if (TestStepViewModel?.TestStep == null) return;
        
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
        
        Dispatcher.UIThread.Post(UpdateStringProperties);
    }

    partial void OnDelayTextChanged(string? value)
    {
        if (TestStepViewModel?.TestStep == null) return;
        
        if (UnitParser.TryParse(value, out var result, "s"))
        {
            if (result < 0) result = 0;
            TestStepViewModel.TestStep.Delay = (int)Math.Round(result * 1000);
        }
        else
        {
            TestStepViewModel.TestStep.Delay = 0;
        }
            
        Dispatcher.UIThread.Post(UpdateStringProperties);
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

    [RelayCommand]
    private async Task OpenImagePicker()
    {
        var result = await _fileDialogService.OpenFileAsync("Select Image", new[] { "png", "jpg", "jpeg", "bmp", "gif", "webp" });
        if (result == null || TestStepViewModel == null) return;
            TestStepViewModel.TestStep.CustomMessageBoxImagePath = result.Path.LocalPath;
    }
}