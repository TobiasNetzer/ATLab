using System;
using System.Reflection;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class AboutWindowViewModel : ViewModelBase
{
    public string FirmwareVersion { get; }
    public string DeviceName { get; }
    public string BuildDate { get; }
    public string BuildTime { get; set; }

    public string MeasChannelCount { get; }
    public string StimChannelCount { get; }
    public string ExtStimChannelCount { get; }

    public string AppVersion => Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
    public string Author => Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Unknown";

    public event Action? RequestClose;

    public AboutWindowViewModel(IHardwareInfoProvider hardware)
    {
        FirmwareVersion = hardware.FirmwareVersion;
        DeviceName  = hardware.DeviceName;
        BuildDate = hardware.BuildDate;
        BuildTime = hardware.BuildTime;
        MeasChannelCount = hardware.MeasChannelCount.ToString();
        StimChannelCount = hardware.StimChannelCount.ToString();
        ExtStimChannelCount = hardware.ExtStimChannelCount.ToString();
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }

}