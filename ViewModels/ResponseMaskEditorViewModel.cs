using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ATLab.Enums;
using ATLab.Helpers;
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
    
    [ObservableProperty]
    private ResponseDisplayMode _responseDisplayMode = ResponseDisplayMode.ASCII;
    
    public List<ResponseDisplayMode> DisplayModes { get; } = Enum.GetValues<ResponseDisplayMode>().ToList();
    
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

        _currentTestStep.ResponseMask.Length = int.TryParse(value, out var parsed)
            ? parsed
            : 0;
        
        Dispatcher.UIThread.Post(UpdateStringProperties);
    }

    partial void OnSkipCharactersChanged(string value)
    {
        if (_currentTestStep?.ResponseMask == null)
            return;

        _currentTestStep.ResponseMask.Skip = int.TryParse(value, out var parsed)
            ? parsed
            : 0;
        
        Dispatcher.UIThread.Post(UpdateStringProperties);
    }

    partial void OnResponseDisplayModeChanged(ResponseDisplayMode value)
    {
        if (_currentTestStep?.ResponseMask == null)
            return;
        
        _currentTestStep.ResponseMask.ResponseDisplayMode = value;
        _currentTestStep.ResponseMask.OriginalResponse = ResponseProcessor.Format(_currentTestStep.ResponseMask.RawOriginal, _currentTestStep.ResponseMask.ResponseDisplayMode);
        _currentTestStep.ResponseMask.ProcessedInput = ResponseProcessor.Format(_currentTestStep.ResponseMask.RawProcessed, _currentTestStep.ResponseMask.ResponseDisplayMode);
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
        FinalResult = _currentTestStep.ResponseMask.Result;
        ResponseDisplayMode = _currentTestStep.ResponseMask.ResponseDisplayMode;
        
        OnPropertyChanged(nameof(Mask));
        OnPropertyChanged(nameof(Length));
        OnPropertyChanged(nameof(SkipCharacters));
        OnPropertyChanged(nameof(OriginalResponse));
        OnPropertyChanged(nameof(ProcessedInput));
        OnPropertyChanged(nameof(FinalResult));
        OnPropertyChanged(nameof(ResponseDisplayMode));
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