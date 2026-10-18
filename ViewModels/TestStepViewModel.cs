using System.ComponentModel;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;
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
    }
}