using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.Analyzer.ViewModels;

/// <summary>Version, build and runtime information, like ATLab's About tab.</summary>
public partial class AboutViewModel
{
    public const string RepositoryUrl = "https://github.com/TobiasNetzer/ATLab";
    public const string LicenseUrl = "https://github.com/TobiasNetzer/ATLab/blob/main/LICENSE";

    private static Assembly? App => Assembly.GetEntryAssembly();

    public string AppName { get; } = App?.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "ATLab Analyzer";

    public string AppVersion { get; } =
        App?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "Unknown";

    public string Copyright { get; } = App?.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";

    public string BuildDate { get; } =
        App?.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "BuildDate")?.Value ?? "Unknown";

    public string BuildConfiguration =>
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    public string AvaloniaVersion { get; } = typeof(Avalonia.Application).Assembly.GetName().Version?.ToString() ?? "Unknown";

    public string RuntimeFramework => RuntimeInformation.FrameworkDescription;

    public string OSInfo =>
        $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture}, {RuntimeInformation.ProcessArchitecture})";

    [RelayCommand]
    private static void OpenLink(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            // No browser registered: nothing sensible to do
        }
    }
}