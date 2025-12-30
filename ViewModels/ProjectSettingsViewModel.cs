using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class ProjectSettingsViewModel : ViewModelBase
{
    private readonly TestStepConfiguratorViewModel _testStepConfiguratorViewModel;
    
    [ObservableProperty]
    private double _toleranceValue;
    
    public event Action? ConfigurationChanged;
    
    public ProjectSettingsViewModel(TestStepConfiguratorViewModel testStepConfiguratorViewModel)
    {
        _testStepConfiguratorViewModel = testStepConfiguratorViewModel;
        
        ResetToDefault();
    }
    
    public void ResetToDefault()
    {
        ToleranceValue = 10;
    }
    
    partial void OnToleranceValueChanged(double value)
    {
        _testStepConfiguratorViewModel.Tolerance = value / 100.0;
        ConfigurationChanged?.Invoke();
    }
}