using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class AboutTabViewModel : ViewModelBase
{
    public static string AppName =>
        Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyProductAttribute>()?
            .Product
        ?? "Unknown";
    
    public static string AppVersion =>
        Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
        ?? "Unknown";

    public static string Copyright =>
        Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyCopyrightAttribute>()?
            .Copyright
        ?? "Unknown";
    
    public string BuildConfiguration =>
        #if DEBUG
                "Debug";
        #else
            "Release";
        #endif
    
    public static string BuildDate =>
        Assembly.GetEntryAssembly()?
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "BuildDate")?
            .Value
        ?? "Unknown";
    
    public string RuntimeFramework =>
        System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

    private string OSDescription =>
        System.Runtime.InteropServices.RuntimeInformation.OSDescription;

    private string OSArchitecture =>
        System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();

    private string ProcessArchitecture =>
        System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();

    public string AvaloniaVersion =>
        typeof(Avalonia.Application).Assembly.GetName().Version?.ToString() ?? "Unknown";
    
    public string OSInfo =>
        $"{OSDescription} ({OSArchitecture}, {ProcessArchitecture})";
    
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