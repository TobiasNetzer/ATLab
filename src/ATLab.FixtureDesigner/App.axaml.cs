using System.Linq;
using ATLab.FixtureDesigner.Localization;
using ATLab.FixtureDesigner.Services;
using ATLab.Pcb.Controls;
using ATLab.FixtureDesigner.ViewModels;
using ATLab.FixtureDesigner.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using QuestPDF.Infrastructure;

namespace ATLab.FixtureDesigner;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <summary>A fixture or board file given on the command line is opened when the window opens.</summary>
    public override void OnFrameworkInitializationCompleted()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // The board view of ATLab.Pcb shows its texts in the language of the Fixture Designer
        BoardTexts.Lookup = key => Localizer.Get(key) is { } text && !text.StartsWith('[') ? text : null;
        Localizer.Instance.CultureChanged += (_, _) => BoardTexts.Refresh();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Language and theme before the window is created, so it opens in the right ones
            var settings = DesignerSettings.Load();
            RequestedThemeVariant = settings.IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
            var viewModel = new MainWindowViewModel(settings);
            var window = new MainWindow { DataContext = viewModel };
            viewModel.Dialogs = new WindowDialogs(window);

            var path = desktop.Args?.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a));
            if (path is not null)
                window.Opened += async (_, _) => await viewModel.OpenPathAsync(path);

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}