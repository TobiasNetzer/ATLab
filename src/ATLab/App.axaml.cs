using System;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Services;
using ATLab.ViewModels;
using ATLab.Views;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace ATLab;

/// <summary>
/// Application entry: builds the dependency injection container, applies theme and language and connects to the
/// test hardware before the main window opens.
/// </summary>
public class App : Application
{
    private IServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Connects to the adapter used last time; if that fails the hardware selection window decides between a port
    /// and simulation mode.
    /// </summary>
    public override async void OnFrameworkInitializationCompleted()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        
        var serviceCollection = new ServiceCollection();

        ConfigureServices(serviceCollection);

        _services = serviceCollection.BuildServiceProvider();
        
        ServiceCollectionExtensions.RegisterDialogs(_services);
        
        var settingsService = _services.GetRequiredService<ISettingsService>();
        
        var localizationService = _services.GetRequiredService<ILocalizationService>();

        if (!string.IsNullOrWhiteSpace(settingsService.Settings.Language))
        {
            localizationService.ChangeCulture(new CultureInfo(settingsService.Settings.Language));
        }

        Current!.RequestedThemeVariant = settingsService.Settings.IsDarkMode
            ? ThemeVariant.Dark
            : ThemeVariant.Light;

        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        TestHardwareConnectWindow? connectWindow = null;

        var hardwareInitializer = _services.GetRequiredService<IHardwareInitializer>();

        var initSuccess = await hardwareInitializer.InitializeAsync();

        if (!initSuccess)
        {
            var connectionResult = await ShowHardwareConnectionWindowAsync(desktop);

            connectWindow = connectionResult.Window;

            if (connectionResult.Connected is null)
            {
                desktop.Shutdown();
                return;
            }

            ApplyConnectionMode(connectionResult.Connected.Value);
        }

        var mainWindow = _services.GetRequiredService<MainWindow>();

        var mainVm = (MainWindowViewModel)mainWindow.DataContext!;

        mainWindow.Opened += async (_, _) => await mainVm.OnWindowOpened();

        desktop.Exit += (_, _) =>
        {
            OnExitAsync().GetAwaiter().GetResult();
        };

        desktop.MainWindow = mainWindow;

        mainWindow.Show();

        connectWindow?.Close();

        base.OnFrameworkInitializationCompleted();
    }

    private async Task<(bool? Connected, TestHardwareConnectWindow Window)> ShowHardwareConnectionWindowAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var window = _services!.GetRequiredService<TestHardwareConnectWindow>();

        desktop.MainWindow = window;

        var vm = (TestHardwareConnectWindowViewModel)window.DataContext!;

        var tcs = new TaskCompletionSource<bool?>(TaskCreationOptions.RunContinuationsAsynchronously);

        void ConnectedHandler(bool connected)
        {
            Cleanup();
            tcs.TrySetResult(connected);
        }

        void ClosedHandler(object? sender, EventArgs e)
        {
            Cleanup();
            tcs.TrySetResult(null);
        }

        void Cleanup()
        {
            vm.Connected -= ConnectedHandler;
            window.Closed -= ClosedHandler;
        }

        vm.Connected += ConnectedHandler;
        window.Closed += ClosedHandler;

        window.Show();

        return (await tcs.Task, window);
    }

    private void ApplyConnectionMode(bool connected)
    {
        var applicationState = _services!.GetRequiredService<ApplicationState>();

        applicationState.IsSimulationMode = !connected;

        if (!connected)
        {
            var hardwareAccessor = _services!.GetRequiredService<IHardwareAccessor>();

            hardwareAccessor.Hardware = _services!.GetRequiredService<TestHardwareSimulator>();
        }
    }


    private async Task OnExitAsync()
    {
        var hardwareAccessor = _services?.GetService<IHardwareAccessor>();
        
        if (hardwareAccessor?.Hardware is IAsyncDisposable asyncHardware)
        {
            await asyncHardware.DisposeAsync();
        }
        
        switch (_services)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync();
                break;
            
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddBackendServices();
        services.AddViewModels();
        services.AddViews();
    }
}