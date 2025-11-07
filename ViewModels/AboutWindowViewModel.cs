using System;
using System.Reflection;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class AboutWindowViewModel : ViewModelBase
{
    private readonly IDeviceInfoProvider _device;

    public string? FirmwareVersion => _device.FirmwareVersion;
    public string? DeviceName => _device.DeviceName;
    public string? BuildDate => _device.BuildDate;
    public string? BuildTime => _device.BuildTime;

    public string AppVersion => Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
    public string Author => Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Unknown";

    public event Action? RequestClose;

    public AboutWindowViewModel(IDeviceInfoProvider device)
    {
        _device = device;
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }

}