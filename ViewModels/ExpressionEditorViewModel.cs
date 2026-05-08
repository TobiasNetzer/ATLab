using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class ExpressionEditorViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    
    [ObservableProperty]
    private bool _isExpanded;
    
    [ObservableProperty]
    private TestStep? _testStep;
    
    public ExpressionEditorViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        
        IsExpanded = settingsService.Settings.IsExpressionEditorExpanded;
    }
    
    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsExpressionEditorExpanded = value;
    }
    
    public void LoadTestStep(TestStep step)
    {
        TestStep = step;
    }
}