using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Helpers;
using ATLab.Interfaces;
using ATLab.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class TestStepConfiguratorViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IFileDialogService _fileDialogService;
    private readonly ProjectSettings _projectSettings;
    private readonly RuntimeVariableEditorViewModel _runtimeVariableEditorViewModel;

    public List<TestEvaluationSource> EvaluationSources { get; } = Enum.GetValues<TestEvaluationSource>().ToList();
        
    [ObservableProperty]
    private TestStepViewModel? _testStepViewModel;
    
    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private string _nominalValueText = string.Empty;

    [ObservableProperty]
    private string _lowerLimitText = string.Empty;

    [ObservableProperty]
    private string _upperLimitText = string.Empty;

    [ObservableProperty]
    private string _delayText = string.Empty;

    [ObservableProperty]
    private bool _isAdvancedExpanded;

    public ObservableCollection<CustomVariable> RuntimeVariables => _runtimeVariableEditorViewModel.RuntimeVariables;
    
    [ObservableProperty]
    private CustomVariable? _selectedVariable;
    
    [ObservableProperty]
    private List<JumpTargetDto> _jumpTargets = new();
    
    [ObservableProperty]
    private JumpTargetDto? _selectedPassJumpTarget;

    [ObservableProperty]
    private JumpTargetDto? _selectedFailJumpTarget;

    private bool _isInternalChange;

    public TestStepConfiguratorViewModel(
        ISettingsService settingsService,
        IFileDialogService fileDialogService,
        ProjectSettings projectSettings,
        RuntimeVariableEditorViewModel runtimeVariableEditorViewModel)
    {
        _settingsService = settingsService;
        _fileDialogService = fileDialogService;
        _projectSettings = projectSettings;
        _runtimeVariableEditorViewModel = runtimeVariableEditorViewModel;
        
        IsExpanded = settingsService.Settings.IsStepConfiguratorExpanded;
    }

    public void LoadTestStep(TestStepViewModel testStep, IEnumerable<TestStepViewModel> allSteps)
    {
        _isInternalChange = true;
        try
        {
            if (TestStepViewModel?.TestStep != null)
            {
                TestStepViewModel.TestStep.PropertyChanged -= TestStepPropertyChanged;
            }

            TestStepViewModel = testStep;

            if (TestStepViewModel?.TestStep == null) return;

            TestStepViewModel.TestStep.PropertyChanged += TestStepPropertyChanged;

            InitializeNumericFromExpressions();
            UpdateStringProperties();

            SelectedVariable = RuntimeVariables.FirstOrDefault(v => v.Name == TestStepViewModel.TestStep.VariableName);

            JumpTargets = allSteps
                .Select(s => new JumpTargetDto(s.TestStep.Id, s.TestStep.Number, s.TestStep.Name))
                .ToList();

            SelectedPassJumpTarget =
                JumpTargets.FirstOrDefault(j => j.Id == TestStepViewModel.TestStep.OnPass.JumpToId);

            SelectedFailJumpTarget =
                JumpTargets.FirstOrDefault(j => j.Id == TestStepViewModel.TestStep.OnFail.JumpToId);
        }
        finally
        {
            _isInternalChange = false;
        }
    }
    
    private void InitializeNumericFromExpressions()
    {
        if (TestStepViewModel?.TestStep == null) return;
        var step = TestStepViewModel.TestStep;
        
        if (!string.IsNullOrWhiteSpace(step.NominalValueExpression) && 
            !step.NominalValueExpression.Contains("{") &&
            UnitParser.TryParse(step.NominalValueExpression, out var nominal, step.Unit))
        {
            step.NominalValue = nominal;
        }
        
        if (!string.IsNullOrWhiteSpace(step.LowerLimitExpression) &&
            !step.LowerLimitExpression.Contains("{") &&
            UnitParser.TryParse(step.LowerLimitExpression, out var lower, step.Unit))
        {
            step.LowerLimit = lower;
        }
        
        if (!string.IsNullOrWhiteSpace(step.UpperLimitExpression) &&
            !step.UpperLimitExpression.Contains("{") &&
            UnitParser.TryParse(step.UpperLimitExpression, out var upper, step.Unit))
        {
            step.UpperLimit = upper;
        }
        
        if (!string.IsNullOrWhiteSpace(step.DelayExpression) &&
            !step.DelayExpression.Contains("{") &&
            UnitParser.TryParse(step.DelayExpression, out var delaySeconds, "s"))
        {
            step.Delay = (int)Math.Round(delaySeconds * 1000);
        }
    }

    private void UpdateStringProperties()
    {
        if (TestStepViewModel?.TestStep == null) return;
        var step = TestStepViewModel.TestStep;

       
        if (!IsVariableExpression(step.NominalValueExpression) && 
            UnitParser.TryParse(step.NominalValueExpression, out var nominal, step.Unit))
        {
            NominalValueText = !string.IsNullOrWhiteSpace(step.Unit) ? UnitParser.Format(nominal, step.Unit) : nominal.ToString(CultureInfo.CurrentCulture);
        }
        else
        {
            NominalValueText = step.NominalValueExpression;
        }
        
        if (!IsVariableExpression(step.LowerLimitExpression) &&
            UnitParser.TryParse(step.LowerLimitExpression, out var lower, step.Unit))
        {
            LowerLimitText = !string.IsNullOrWhiteSpace(step.Unit) ? UnitParser.Format(lower, step.Unit) : lower.ToString(CultureInfo.CurrentCulture);
        }
        else
        {
            LowerLimitText = step.LowerLimitExpression;
        }
        
        if (!IsVariableExpression(step.UpperLimitExpression) &&
            UnitParser.TryParse(step.UpperLimitExpression, out var upper, step.Unit))
        {
            UpperLimitText = !string.IsNullOrWhiteSpace(step.Unit) ? UnitParser.Format(upper, step.Unit) : upper.ToString(CultureInfo.CurrentCulture);
        }
        else
        {
            UpperLimitText = step.UpperLimitExpression;
        }
        
        if (!IsVariableExpression(step.DelayExpression) &&
            UnitParser.TryParse(step.DelayExpression, out var delaySec, "s"))
        {
            DelayText = UnitParser.Format(delaySec, "s");
        }
        else
        {
            DelayText = step.DelayExpression;
        }
        
        OnPropertyChanged(nameof(NominalValueText));
        OnPropertyChanged(nameof(LowerLimitText));
        OnPropertyChanged(nameof(UpperLimitText));
        OnPropertyChanged(nameof(DelayText));
    }

    private void TestStepPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (TestStepViewModel?.TestStep == null) return;
        var step = TestStepViewModel.TestStep;

        switch (e.PropertyName)
        {
            case nameof(TestStepViewModel.TestStep.NominalValue):
                // Only auto‑tolerance when nominal is NOT an expression
                // and lower/upper are NOT expressions.
                if (!IsVariableExpression(step.NominalValueExpression) &&
                    !IsVariableExpression(step.LowerLimitExpression) &&
                    !IsVariableExpression(step.UpperLimitExpression))
                {
                    UpdateLimitsFromNominal();
                }
                else if (!IsVariableExpression(step.NominalValueExpression))
                {
                    // Even if we don't auto-calculate from tolerance, 
                    // we must ensure literal limits are still valid relative to the new nominal
                    EnforceLimitConstraints(step);
                }
                UpdateStringProperties();
                break;

            case nameof(TestStepViewModel.TestStep.LowerLimit):
            case nameof(TestStepViewModel.TestStep.UpperLimit):
            case nameof(TestStepViewModel.TestStep.Delay):
            case nameof(TestStepViewModel.TestStep.Unit):
            case nameof(TestStepViewModel.TestStep.NominalValueExpression):
            case nameof(TestStepViewModel.TestStep.LowerLimitExpression):
            case nameof(TestStepViewModel.TestStep.UpperLimitExpression):
            case nameof(TestStepViewModel.TestStep.DelayExpression):
                UpdateStringProperties();
                break;
        }
    }

    private static bool IsVariableExpression(string text)
        => text.Contains("{");

    partial void OnNominalValueTextChanged(string? oldValue, string newValue)
    {
        if (TestStepViewModel?.TestStep == null || _isInternalChange) return;
        if (newValue == oldValue) return;
        var step = TestStepViewModel.TestStep;

        step.NominalValueExpression = newValue;
        
        if (!IsVariableExpression(newValue))
        {
            if (UnitParser.TryParse(newValue, out var result, step.Unit))
            {
                step.NominalValue = result;
                step.NominalValueExpression = result.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                step.NominalValue = 0;
            }
        }
        
        Dispatcher.UIThread.Post(UpdateStringProperties);
    }

    partial void OnLowerLimitTextChanged(string value)
    {
        if (TestStepViewModel?.TestStep == null || _isInternalChange) return;
        var step = TestStepViewModel.TestStep;

        step.LowerLimitExpression = value;
        
        if (!IsVariableExpression(value))
        {
            if (IsVariableExpression(step.NominalValueExpression))
            {
                if (UnitParser.TryParse(value, out var parsed, step.Unit))
                {
                    step.LowerLimit = parsed;
                    step.LowerLimitExpression = parsed.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    step.LowerLimit = 0;
                }
            }
            else
            {
                if (UnitParser.TryParse(value, out var result, step.Unit))
                {
                    var nominal = step.NominalValue;
                    step.LowerLimit = Math.Min(result, nominal);
                    step.LowerLimitExpression = step.LowerLimit.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    step.LowerLimit = 0;
                }
            }
        }

        Dispatcher.UIThread.Post(UpdateStringProperties);
    }


    partial void OnUpperLimitTextChanged(string value)
    {
        if (TestStepViewModel?.TestStep == null || _isInternalChange) return;
        var step = TestStepViewModel.TestStep;

        step.UpperLimitExpression = value;
        
        if (!IsVariableExpression(value))
        {
            if (IsVariableExpression(step.NominalValueExpression))
            {
                if (UnitParser.TryParse(value, out var parsed, step.Unit))
                {
                    step.UpperLimit = parsed;
                    step.UpperLimitExpression = parsed.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    step.UpperLimit = 0;
                }
            }
            else
            {
                if (UnitParser.TryParse(value, out var result, step.Unit))
                {
                    var nominal = step.NominalValue;
                    step.UpperLimit = Math.Max(result, nominal);
                    step.UpperLimitExpression = step.UpperLimit.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    step.UpperLimit = 0;
                }
            }
        }

        Dispatcher.UIThread.Post(UpdateStringProperties);
    }

    partial void OnDelayTextChanged(string value)
    {
        if (TestStepViewModel?.TestStep == null || _isInternalChange) return;
        var step = TestStepViewModel.TestStep;
        
        step.DelayExpression = value;

        if (!IsVariableExpression(value))
        {
            if (UnitParser.TryParse(value, out var result, "s"))
            {
                if (result < 0) result = 0;
                step.Delay = (int)Math.Round(result * 1000);
                step.DelayExpression = result.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                step.Delay = 0;
            }
        }

        Dispatcher.UIThread.Post(UpdateStringProperties);
    }

    private void EnforceLimitConstraints(TestStep step)
    {
        var nominal = step.NominalValue;
        var changed = false;

        if (!IsVariableExpression(step.LowerLimitExpression) && step.LowerLimit > nominal)
        {
            step.LowerLimit = nominal;
            step.LowerLimitExpression = step.LowerLimit.ToString(CultureInfo.InvariantCulture);
            changed = true;
        }

        if (!IsVariableExpression(step.UpperLimitExpression) && step.UpperLimit < nominal)
        {
            step.UpperLimit = nominal;
            step.UpperLimitExpression = step.UpperLimit.ToString(CultureInfo.InvariantCulture);
            changed = true;
        }
        
        if (changed)
        {
            UpdateStringProperties();
        }
    }

    private void UpdateLimitsFromNominal()
    {
        if (TestStepViewModel?.TestStep == null) return;

        var step = TestStepViewModel.TestStep;
        var nominal = step.NominalValue;
        var tolerance = _projectSettings.ToleranceValue / 100.0;
        var v1 = nominal * (1 + tolerance);
        var v2 = nominal * (1 - tolerance);
        
        step.UpperLimit = Math.Max(Math.Max(v1, v2), nominal);
        step.LowerLimit = Math.Min(Math.Min(v1, v2), nominal);
        
        step.LowerLimitExpression = step.LowerLimit.ToString(CultureInfo.InvariantCulture);
        step.UpperLimitExpression = step.UpperLimit.ToString(CultureInfo.InvariantCulture);
    }

    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsStepConfiguratorExpanded = value;
    }

    partial void OnSelectedVariableChanged(CustomVariable? value)
    {
        if (TestStepViewModel?.TestStep == null || value == null) return;
        TestStepViewModel.TestStep.VariableName = value.Name;
    }

    partial void OnSelectedPassJumpTargetChanged(JumpTargetDto? value)
    {
        if (TestStepViewModel?.TestStep == null || value == null) return;
        TestStepViewModel.TestStep.OnPass.JumpToId = value.Id;
    }

    partial void OnSelectedFailJumpTargetChanged(JumpTargetDto? value)
    {
        if (TestStepViewModel?.TestStep == null || value == null) return;
        TestStepViewModel.TestStep.OnFail.JumpToId = value.Id;
    }

    [RelayCommand]
    private async Task OpenImagePicker()
    {
        var result = await _fileDialogService.OpenFileAsync("Select Image",
            new[] { "png", "jpg", "jpeg", "bmp", "gif", "webp" });

        if (result == null || TestStepViewModel == null) return;

        TestStepViewModel.TestStep.CustomMessageBoxImagePath = result.Path.LocalPath;
    }
}