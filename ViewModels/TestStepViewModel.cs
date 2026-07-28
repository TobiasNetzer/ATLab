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
    
    public string? ResultNoFormatting;
    
    [ObservableProperty]
    private bool _isPassed;

    [ObservableProperty]
    private string? _deviation;

    [ObservableProperty]
    private bool _isExecuted;

    public TestStepViewModel(TestStep testStep, IHardwareInfo hardwareInfo)
    {
        TestStep = testStep;
        
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