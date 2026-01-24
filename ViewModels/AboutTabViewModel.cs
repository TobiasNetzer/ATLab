using System;
using System.Reflection;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class AboutTabViewModel : ViewModelBase
{
    public string FirmwareVersion { get; }
    public string DeviceName { get; }
    public string SerialNumber { get; }
    public string BuildDate { get; }
    public string BuildTime { get; }

    public int MeasChannelCount { get; }
    public int StimChannelCount { get; }
    public int ExtStimChannelCount { get; }

    public static string AppVersion =>
        Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
        ?? "Unknown";

    public static string Author =>
        Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyCompanyAttribute>()?
            .Company
        ?? "Unknown";
    
    public string BuildConfiguration =>
#if DEBUG
        "Debug";
#else
    "Release";
#endif

    public string TargetFramework =>
        System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

    public string RuntimeVersion =>
        Environment.Version.ToString();

    public string OSDescription =>
        System.Runtime.InteropServices.RuntimeInformation.OSDescription;

    public string OSArchitecture =>
        System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();

    public string ProcessArchitecture =>
        System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();

    public string AvaloniaVersion =>
        typeof(Avalonia.Application).Assembly.GetName().Version?.ToString() ?? "Unknown";

    
    public AboutTabViewModel(IHardwareInfo hardwareInfo)
    {
        FirmwareVersion = hardwareInfo.FirmwareVersion;
        DeviceName = hardwareInfo.DeviceName;
        SerialNumber = hardwareInfo.SerialNumber;
        BuildDate = hardwareInfo.BuildDate;
        BuildTime = hardwareInfo.BuildTime;

        MeasChannelCount = hardwareInfo.MeasChannelCount;
        StimChannelCount = hardwareInfo.StimChannelCount;
        ExtStimChannelCount = hardwareInfo.ExtStimChannelCount;

        Title = "About";
    }
}