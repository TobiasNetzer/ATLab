using System;
using System.Reflection;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class AboutWindowViewModel : ViewModelBase
{
    private readonly IHardwareInfoProvider _hardware;

    public string? FirmwareVersion => _hardware.FirmwareVersion;
    public string? DeviceName => _hardware.DeviceName;
    public string? BuildDate => _hardware.BuildDate;
    public string? BuildTime => _hardware.BuildTime;

    public string? MeasChannelCount => _hardware.MeasChannelCount.ToString();
    public string? StimChannelCount => _hardware.StimChannelCount.ToString();
    public string? ExtStimChannelCount => _hardware.ExtStimChannelCount.ToString();

    public string AppVersion => Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
    public string Author => Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Unknown";

    public event Action? RequestClose;

    public AboutWindowViewModel(IHardwareInfoProvider hardware)
    {
        _hardware = hardware;
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }

}