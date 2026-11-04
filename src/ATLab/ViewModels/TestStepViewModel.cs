using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ATLab.Services;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;
/// <summary>A step in the step list with the result of its last execution.</summary>
public partial class TestStepViewModel : ViewModelBase
{
    [ObservableProperty]
    private TestStep _testStep;

    [ObservableProperty]
    private string? _result;
    
    [ObservableProperty]
    private string? _resultNoFormatting;
    
    [ObservableProperty]
    private bool _isPassed;

    [ObservableProperty]
    private string? _deviation;

    [ObservableProperty]
    private bool _isExecuted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResultDetails), nameof(ResultDetails))]
    private IReadOnlyList<string> _resultDetailLines = [];

    public bool HasResultDetails => ResultDetailLines.Count > 0;

    // A response check of the step's script failed: the step fails even if its value is within the limits
    public bool IsResponseCheckFailed { get; set; }
    
    // All detail lines, shown in the result details dialog
    public string? ResultDetails => ResultDetailLines.Count == 0
        ? null
        : string.Join(Environment.NewLine, ResultDetailLines);

    // The module the step belongs to, used for navigation during execution and in reports
    public TestModule Module { get; }

    public TestStepViewModel(TestStep testStep, TestModule module, IHardwareInfo hardwareInfo)
    {
        TestStep = testStep;
        Module = module;
        
        TestStep.InitializeRuntimeState(hardwareInfo);
    }

    partial void OnTestStepChanged(TestStep? oldValue, TestStep newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= TestStep_PropertyChanged;
        newValue.PropertyChanged += TestStep_PropertyChanged;
    }
    
    private void TestStep_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);
    }

    public void ResetResults()
    {
        Result = null;
        ResultNoFormatting = null;
        Deviation = null;
        IsPassed  = false;
        IsExecuted = false;
        ResultDetailLines = [];
        IsResponseCheckFailed = false;
    }
}