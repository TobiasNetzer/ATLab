using System;
using System.Reflection;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class AboutWindowViewModel(IHardwareInfoProvider hardwareInfo) : ViewModelBase
{
    public string FirmwareVersion { get; } = hardwareInfo.FirmwareVersion;
    public string DeviceName { get; } = hardwareInfo.DeviceName;
    public string BuildDate { get; } = hardwareInfo.BuildDate;
    public string BuildTime { get; set; } = hardwareInfo.BuildTime;

    public string MeasChannelCount { get; } = hardwareInfo.MeasChannelCount.ToString();
    public string StimChannelCount { get; } = hardwareInfo.StimChannelCount.ToString();
    public string ExtStimChannelCount { get; } = hardwareInfo.ExtStimChannelCount.ToString();

    public static string AppVersion => Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
    public static string Author => Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Unknown";

    public event Action? RequestClose;

    [RelayCommand]
    private void Close() => RequestClose?.Invoke();

}