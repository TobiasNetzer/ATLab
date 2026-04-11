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
    private string _mask = string.Empty;

    [ObservableProperty]
    private string _length = string.Empty;

    [ObservableProperty]
    private string _skipCharacters = string.Empty;

    [ObservableProperty]
    private bool _isOnlyNumeric;

    [ObservableProperty]
    private string _lastOriginalResponse = string.Empty;

    [ObservableProperty]
    private string _lastProcessedInput = string.Empty;

    [ObservableProperty]
    private string _lastFinalResult = string.Empty;
    
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

    partial void OnIsOnlyNumericChanged(bool value)
    {
        if (_currentTestStep?.ResponseMask != null)
            _currentTestStep.ResponseMask.IsOnlyNumeric = value;
    }

    private void UpdateStringProperties()
    {
        if (_currentTestStep == null)
            return;

        Mask = _currentTestStep.ResponseMask.Mask;
        Length = _currentTestStep.ResponseMask.Length.ToString();
        SkipCharacters = _currentTestStep.ResponseMask.Skip.ToString();
        IsOnlyNumeric = _currentTestStep.ResponseMask.IsOnlyNumeric;
        LastOriginalResponse = _currentTestStep.ResponseMask.LastOriginalResponse;
        LastProcessedInput = _currentTestStep.ResponseMask.LastProcessedInput;
        LastFinalResult = _currentTestStep.ResponseMask.LastFinalResult;
        
        OnPropertyChanged(nameof(Mask));
        OnPropertyChanged(nameof(Length));
        OnPropertyChanged(nameof(SkipCharacters));
        OnPropertyChanged(nameof(IsOnlyNumeric));
        OnPropertyChanged(nameof(LastOriginalResponse));
        OnPropertyChanged(nameof(LastProcessedInput));
        OnPropertyChanged(nameof(LastFinalResult));
    }

    public void LoadTestStep(TestStepViewModel? testStepViewModel)
    {
        if (_currentTestStep?.ResponseMask != null)
            _currentTestStep.ResponseMask.PropertyChanged -= ResponseMaskChanged;

        _currentTestStep = testStepViewModel?.TestStep;

        if (_currentTestStep == null)
            return;

        Mask = _currentTestStep.ResponseMask.Mask;
        Length = _currentTestStep.ResponseMask.Length.ToString();
        SkipCharacters = _currentTestStep.ResponseMask.Skip.ToString();
        IsOnlyNumeric = _currentTestStep.ResponseMask.IsOnlyNumeric;
        LastOriginalResponse = _currentTestStep.ResponseMask.LastOriginalResponse;
        LastProcessedInput = _currentTestStep.ResponseMask.LastProcessedInput;
        LastFinalResult = _currentTestStep.ResponseMask.LastFinalResult;
        
        _currentTestStep.ResponseMask.PropertyChanged += ResponseMaskChanged;

        UpdateStringProperties();
    }

    private void ResponseMaskChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateStringProperties();
    }

}