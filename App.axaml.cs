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

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var initSuccess = false;
            var openConnectWindow = false;
            var lastPort = settingsService.Settings.LastComPort;
            var factory = _services.GetRequiredService<ICommunicationFactory>();
            var hardwareAccessor = _services.GetRequiredService<IHardwareAccessor>();

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
                    var communication = ActivatorUtilities.CreateInstance<CtiaCommunication>(_services!, testHardwareInterface);
                    var hardware = ActivatorUtilities.CreateInstance<CtiaHardware>(_services!, communication);
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
                var serialPortWindow = _services.GetRequiredService<TestHardwareConnectWindow>();
                var tcs = new TaskCompletionSource<bool?>();

                var vm = (TestHardwareConnectWindowViewModel)serialPortWindow.DataContext!;
                vm.Connected += connectionStatus =>
                {
                    tcs.TrySetResult(connectionStatus);
                    serialPortWindow.Close();
                };

                serialPortWindow.Closed += (_, _) => tcs.TrySetResult(null);

                serialPortWindow.Show();

                var result = await tcs.Task;

                if (result != null)
                {
                    initSuccess = true;

                    if (result == false)
                    {
                        hardwareAccessor.Hardware = _services.GetRequiredService<TestHardwareSimulator>();
                        _services.GetRequiredService<ISimulationService>().IsSimulationMode = true;
                    }
                    else
                    {
                        _services.GetRequiredService<ISimulationService>().IsSimulationMode = false;
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
            window.Opened += async (_, __) => await mainVm.OnWindowOpened();
            
            desktop.Exit += async (_, __) => await OnExitAsync();
            
            desktop.MainWindow = window;
            window.Show();

            base.OnFrameworkInitializationCompleted();
        }
    }

    private async Task OnExitAsync()
    {
        if (_services is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (_services is IDisposable disposable)
            disposable.Dispose();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddBackendServices();
        services.AddViewModels();
        services.AddViews();
    }
}