using System;
using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class AboutTabViewModel : ViewModelBase
{
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
    
    public AboutTabViewModel()
    {
        Title = "About";
    }

    [RelayCommand]
    void OpenLink(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
}