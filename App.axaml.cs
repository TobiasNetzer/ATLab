using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ATLab.ViewModels;
using ATLab.Views;
using ATLab.Services;
using System.Threading.Tasks;
using ATLab.CTIA;
using ATLab.Interfaces;
using ATLab.Models;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace ATLab;

public class App : Application
{
    private IServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        _services = serviceCollection.BuildServiceProvider();
        
        var settingsService = _services.GetRequiredService<ISettingsService>();
        
        Current!.RequestedThemeVariant = settingsService.Settings.IsDarkMode
            ? ThemeVariant.Dark
            : ThemeVariant.Light;

        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var initSuccess = false;
        var openConnectWindow = false;

        var lastPort = settingsService.Settings.LastComPort;
        var factory = _services.GetRequiredService<ICommunicationFactory>();
        var hardwareAccessor = _services.GetRequiredService<IHardwareAccessor>();
        
        TestHardwareConnectWindow? serialPortWindow = null;

        if (string.IsNullOrEmpty(lastPort))
        {
            openConnectWindow = true;
        }
        else
        {
            var testHardwareInterface = factory.CreateSerial(lastPort, new DeviceConfiguration());

            var openResult = await testHardwareInterface.ConnectAsync();

            if (!openResult.IsSuccess)
            {
                await testHardwareInterface.DisconnectAsync();
                await testHardwareInterface.DisposeAsync();
                openConnectWindow = true;
            }
            else
            {
                var communication = ActivatorUtilities.CreateInstance<CtiaCommunication>(_services, testHardwareInterface);
                var hardware = ActivatorUtilities.CreateInstance<CtiaHardware>(_services, communication);
                var initResult = await hardware.InitializeAsync();
                if (!initResult.IsSuccess)
                {
                    await testHardwareInterface.DisconnectAsync();
                    await testHardwareInterface.DisposeAsync();
                    openConnectWindow = true;
                }
                else
                {
                    hardwareAccessor.Hardware = hardware;
                    initSuccess = true;
                }
            }
        }

        if (openConnectWindow)
        {
            serialPortWindow = _services.GetRequiredService<TestHardwareConnectWindow>();
            
            desktop.MainWindow = serialPortWindow;

            var vm = (TestHardwareConnectWindowViewModel)serialPortWindow.DataContext!;

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
                serialPortWindow.Closed -= ClosedHandler;
            }

            vm.Connected += ConnectedHandler;
            serialPortWindow.Closed += ClosedHandler;

            serialPortWindow.Show();

            var result = await tcs.Task;

            if (result is not null)
            {
                initSuccess = true;

                var applicationState = _services.GetRequiredService<ApplicationState>();

                applicationState.IsSimulationMode = result != true;

                if (result != true)
                {
                    hardwareAccessor.Hardware = _services.GetRequiredService<TestHardwareSimulator>();
                }
            }
        }

        if (!initSuccess)
        {
            desktop.Shutdown();
            return;
        }

        var window = _services.GetRequiredService<MainWindow>();
        var mainVm = (MainWindowViewModel)window.DataContext!;
        
        window.Opened += async (_, _) => await mainVm.OnWindowOpened();

        desktop.Exit += (_, _) =>
        {
            OnExitAsync().GetAwaiter().GetResult();
        };

        desktop.MainWindow = window;
        window.Show();

        serialPortWindow?.Close();

        base.OnFrameworkInitializationCompleted();
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