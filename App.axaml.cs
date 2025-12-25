using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using ATLab.ViewModels;
using ATLab.Views;
using ATLab.Services;
using System.Threading.Tasks;
using ATLab.CTIA;
using ATLab.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ATLab;

public class App : Application
{
    private IServiceProvider Services { get; set; } = null!;
    
    private IErrorService _errorService = null!;

    private ITestHardware? _testHardware;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

        _errorService = Services.GetRequiredService<IErrorService>();
        var settingsService = Services.GetRequiredService<ISettingsService>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            
            bool initSuccess = false;
            bool openConnectWindow = false;

            var service = new SerialPortService(settingsService.Settings.LastComPort!);
            var openResult = service.TryOpen();
            if (!openResult.IsSuccess)
            {
                openConnectWindow = true;
                //await ShowInitErrorAndPromptUser(openResult.ErrorMessage, desktop);
            }
            else
            {
                _testHardware = new CtiaHardware(service);
                var initResult = await _testHardware.InitializeAsync();
                if (!initResult.IsSuccess)
                {
                    openConnectWindow = true;
                    //await ShowInitErrorAndPromptUser(initResult.ErrorMessage, desktop);
                }
                else initSuccess = true;
            }

            if (openConnectWindow)
            {
                service.Dispose();
                var serialPortWindow = new SerialPortConnectWindow();
                var tcs = new TaskCompletionSource<bool?>();

                if (new SerialPortConnectWindowViewModel(settingsService) is { } vm)
                {
                    serialPortWindow.DataContext = vm;
                    vm.Connected += connectionStatus =>
                    {
                        tcs.TrySetResult(connectionStatus);
                        serialPortWindow.Close();
                    };

                    vm.RequestClose += () => serialPortWindow.Close();

                    serialPortWindow.Closed += (_, _) => tcs.TrySetResult(null);

                    serialPortWindow.Show();

                    var result = await tcs.Task;

                    if (result != null)
                    {
                        initSuccess = true;

                        if (result == true)
                        {
                            _testHardware = vm.TestHardware!;
                            Services.GetRequiredService<ISimulationService>().IsSimulationMode = false;
                        }
                        else
                        {
                            _testHardware = new CtiaHardware(new SimulationService());
                            Services.GetRequiredService<ISimulationService>().IsSimulationMode = true;
                        }
                    }

                }
            }
            
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            if (!initSuccess)
            {
                desktop.Shutdown();
                return;
            }

            var mainVm = Services.GetRequiredService<MainWindowViewModel>();
            var window = new MainWindow(settingsService, Services.GetRequiredService<IMessageBoxService>()) { DataContext = mainVm };

            desktop.MainWindow = window;
            window.Show();
            
            // Load last open file
            _ = Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var lastFile = settingsService.Settings.LastOpenedFile;

                if (File.Exists(lastFile))
                {
                    try
                    {
                        await mainVm.LoadFile(lastFile);
                    }
                    catch (Exception ex)
                    {
                        _errorService.Errors.Add(ex.ToString());
                    }
                }
            });
            
        }
        
        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IMessageBoxService, MessageBoxService>();
        services.AddSingleton<ISimulationService, SimulationStateService>();
        services.AddSingleton<IErrorService, ErrorService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        
        // Register the runner and executor
        services.AddSingleton<ITestStepRunner, DummyTestStepRunner>();
        services.AddSingleton<ITestExecutor, TestExecutor>();
        
        // Factory for ITestHardware since it's initialized later
        services.AddSingleton<ITestHardware>(sp => _testHardware ?? throw new InvalidOperationException("Hardware not initialized"));
        services.AddSingleton<IHardwareInfo>(sp => sp.GetRequiredService<ITestHardware>().HardwareInfo);

        // ViewModels
        services.AddSingleton<TestConfigurationViewModel>();
            
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<TestingTabViewModel>();
        services.AddTransient<LabTabViewModel>();
        services.AddTransient<ConfigTabViewModel>();
        services.AddTransient<TestStepPresenterViewModel>();
        services.AddTransient<TestStepConfiguratorViewModel>();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}