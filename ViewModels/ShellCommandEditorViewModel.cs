using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class ShellCommandEditorViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    
    public List<ShellCommandOptions> CommandOptions { get; } = Enum.GetValues<ShellCommandOptions>().ToList();
    
    [ObservableProperty]
    private ShellCommand _shellCommand = new();
    
    [ObservableProperty]
    private bool _isExpanded;

    public ShellCommandEditorViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        
        IsExpanded = settingsService.Settings.IsShellCommandEditorExpanded;
    }
    
    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsShellCommandEditorExpanded = value;
    }

    public void LoadTestStep(ShellCommand shellCommand)
    {
        ShellCommand = shellCommand;
    }
}