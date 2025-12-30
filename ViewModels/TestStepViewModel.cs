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
    private bool _isPassed;

    [ObservableProperty]
    private string? _deviation;

    public TestStepViewModel(TestStep testStep, IHardwareInfo hardwareInfo)
    {
        TestStep = testStep;
        
        TestStep.LiveStimState = new RelayGroup(hardwareInfo.StimChannelCount);
        TestStep.LiveStimState.ApplyDto(TestStep.StimState ?? new RelayGroupDto());
        
        TestStep.LiveExtStimState = new RelayGroup(hardwareInfo.ExtStimChannelCount);
        TestStep.LiveExtStimState.ApplyDto(TestStep.ExtStimState ?? new RelayGroupDto());
    }

    partial void OnTestStepChanged(TestStep? oldValue, TestStep newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= TestStep_PropertyChanged;
        newValue.PropertyChanged += TestStep_PropertyChanged;
    }

    private void TestStep_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TestStep));
    }
}
