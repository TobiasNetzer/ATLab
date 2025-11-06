using System;
using ATLab.Models;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class AboutWindowViewModel : ViewModelBase
{
    private readonly CTIAController _cTIA;

    public string? FirmwareVersion => _cTIA.FirmwareVersion;
    public string? DeviceName => _cTIA.DeviceName;
    public string? BuildDate => _cTIA.BuildDate;
    public string? BuildTime => _cTIA.BuildTime;

    public string AppVersion => "0.0.1";
    public string Author => "Tobias Netzer";

    public event Action? RequestClose;

    public AboutWindowViewModel(CTIAController cTIA)
    {
        _cTIA = cTIA;
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }

}