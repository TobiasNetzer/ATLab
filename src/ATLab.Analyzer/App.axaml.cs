using System.Linq;
using ATLab.Analyzer.Services;
using ATLab.Analyzer.ViewModels;
using ATLab.Analyzer.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace ATLab.Analyzer;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <summary>Files and folders given on the command line are imported when the window opens.</summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Language and theme before the window is created, so it opens in the right ones
            var settings = AnalyzerSettings.Load();
            RequestedThemeVariant = settings.IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
            var viewModel = new MainWindowViewModel(settings);
            var window = new MainWindow { DataContext = viewModel };
            viewModel.FileDialogs = new StorageFileDialogs(window);

            var args = desktop.Args?.Where(a => !string.IsNullOrWhiteSpace(a)).ToList() ?? [];
            if (args.Count > 0)
                window.Opened += async (_, _) => await viewModel.LoadPathsAsync(args);

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}