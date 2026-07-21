using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.Enums;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

public partial class TestInterfaceCommunicationViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    
    public List<CommunicationInterfaceType> InterfaceType { get; } = Enum.GetValues<CommunicationInterfaceType>().ToList();
    public List<I2CSpeedMode> I2CSpeedMode { get; } = Enum.GetValues<I2CSpeedMode>().ToList();
    
    [ObservableProperty]
    private bool _isExpanded;
    
    public TestInterfaceCommunicationViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        
        IsExpanded = settingsService.Settings.IsFilePathEditorExpanded;
    }
    
    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsFilePathEditorExpanded = value;
    }
    
}