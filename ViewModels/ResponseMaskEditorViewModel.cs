using System.ComponentModel;
using ATLab.Interfaces;
using ATLab.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class ResponseMaskEditorViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private bool _isExpanded;
    
    [ObservableProperty]
    private bool _isCustomMaskEnabled;

    [ObservableProperty]
    private string _mask = string.Empty;

    [ObservableProperty]
    private string _length = string.Empty;

    [ObservableProperty]
    private string _skipCharacters = string.Empty;

    [ObservableProperty]
    private string _originalResponse = string.Empty;

    [ObservableProperty]
    private string _processedInput = string.Empty;

    [ObservableProperty]
    private string _finalResult = string.Empty;
    
    private TestStep? _currentTestStep;

    public ResponseMaskEditorViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        IsExpanded = settingsService.Settings.IsResponseMaskEditorExpanded;
    }
    
    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsResponseMaskEditorExpanded = value;
    }

    partial void OnIsCustomMaskEnabledChanged(bool value)
    {
        if (_currentTestStep?.ResponseMask != null)
            _currentTestStep.IsCustomMask = value;
    }

    partial void OnMaskChanged(string value)
    {
        if (_currentTestStep?.ResponseMask != null)
            _currentTestStep.ResponseMask.Mask = value;
    }

    partial void OnLengthChanged(string value)
    {
        if (_currentTestStep?.ResponseMask == null)
            return;

        if (int.TryParse(value, out var parsed))
        {
            _currentTestStep.ResponseMask.Length = parsed;
        }
        else
        {
            _currentTestStep.ResponseMask.Length = 0;
        }
        Dispatcher.UIThread.Post(UpdateStringProperties);
    }

    partial void OnSkipCharactersChanged(string value)
    {
        if (_currentTestStep?.ResponseMask == null)
            return;

        if (int.TryParse(value, out var parsed))
        {
            _currentTestStep.ResponseMask.Skip = parsed;
        }
        else
        {
            _currentTestStep.ResponseMask.Skip = 0;
        }
        Dispatcher.UIThread.Post(UpdateStringProperties);
    }

    private void UpdateStringProperties()
    {
        if (_currentTestStep == null)
            return;
    
        IsCustomMaskEnabled = _currentTestStep.IsCustomMask;
        Mask = _currentTestStep.ResponseMask.Mask;
        Length = _currentTestStep.ResponseMask.Length.ToString();
        SkipCharacters = _currentTestStep.ResponseMask.Skip.ToString();
        OriginalResponse = _currentTestStep.ResponseMask.OriginalResponse;
        ProcessedInput = _currentTestStep.ResponseMask.ProcessedInput;
        FinalResult = _currentTestStep.ResponseMask.FinalResult;
        
        OnPropertyChanged(nameof(Mask));
        OnPropertyChanged(nameof(Length));
        OnPropertyChanged(nameof(SkipCharacters));
        OnPropertyChanged(nameof(OriginalResponse));
        OnPropertyChanged(nameof(ProcessedInput));
        OnPropertyChanged(nameof(FinalResult));
    }

    public void LoadTestStep(TestStepViewModel? testStepViewModel)
    {
        if (_currentTestStep?.ResponseMask != null)
            _currentTestStep.ResponseMask.PropertyChanged -= ResponseMaskChanged;

        _currentTestStep = testStepViewModel?.TestStep;

        if (_currentTestStep == null)
            return;
        
        _currentTestStep.ResponseMask.PropertyChanged += ResponseMaskChanged;

        UpdateStringProperties();
    }

    private void ResponseMaskChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateStringProperties();
    }
}