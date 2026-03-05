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
using ATLab.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ATLab;

public class App : Application
{
    private IServiceProvider? _services;
    
    private IErrorService? _errorService;

    private ITestHardware? _testHardware;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        _services = serviceCollection.BuildServiceProvider();

        _errorService = _services.GetRequiredService<IErrorService>();
        var settingsService = _services.GetRequiredService<ISettingsService>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            
            var initSuccess = false;
            var openConnectWindow = false;
            var lastPort = settingsService.Settings.LastComPort;
            var factory = _services.GetRequiredService<ICommunicationFactory>();

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
                    (testHardwareInterface as IDisposable)?.Dispose();
                    openConnectWindow = true;
                }
                else
                {
                    var communication = ActivatorUtilities.CreateInstance<CtiaCommunication>(_services!, testHardwareInterface);
                    _testHardware = ActivatorUtilities.CreateInstance<CtiaHardware>(_services!, communication);
                    var initResult = await _testHardware.InitializeAsync();
                    if (!initResult.IsSuccess)
                    {
                        await testHardwareInterface.DisconnectAsync();
                        (testHardwareInterface as IDisposable)?.Dispose();
                        openConnectWindow = true;
                    }
                    else initSuccess = true;
                }
            }

            if (openConnectWindow)
            {
                var serialPortWindow = _services.GetRequiredService<TestHardwareConnectWindow>();
                var tcs = new TaskCompletionSource<bool?>();

                var vm = _services.GetRequiredService<TestHardwareConnectWindowViewModel>();
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
                        _services.GetRequiredService<ISimulationService>().IsSimulationMode = false;
                    }
                    else
                    {
                        _testHardware = _services.GetRequiredService<TestHardwareSimulator>();
                        _services.GetRequiredService<ISimulationService>().IsSimulationMode = true;
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

            var mainVm = _services.GetRequiredService<MainWindowViewModel>();
            var window = _services.GetRequiredService<MainWindow>();
            window.DataContext = mainVm;

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
                        await mainVm.NewFile();
                    }
                }
                else await mainVm.NewFile();
            });
            
        }
        
        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<ISerialNumberDialogService, SerialNumberDialogService>();
        services.AddSingleton<IMessageBoxService, MessageBoxService>();
        services.AddSingleton<ISimulationService, SimulationStateService>();
        services.AddSingleton<IErrorService, ErrorService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IScriptRepository, FileScriptRepository>();
        services.AddSingleton<IScriptService, ScriptService>();
        services.AddSingleton<IScriptRunner, ScriptRunner>();
        services.AddSingleton<ICommandExecutor, CommandExecutor>();
        services.AddSingleton<ITestStepEvaluator, TestStepEvaluator>();
        services.AddSingleton<IResponseProcessor, ResponseProcessor>();
        services.AddSingleton<IProjectSettings, ProjectSettingsService>();
        services.AddSingleton<CsvExportService>();
        services.AddSingleton<TestResultExportService>();
        
        // Register the runner and executor
        services.AddSingleton<ITestStepRunner, TestStepRunner>();
        services.AddSingleton<ITestExecutor, TestExecutor>();

        services.AddTransient<CtiaCommunication>();
        services.AddTransient<CtiaHardware>();
        services.AddSingleton<TestHardwareSimulator>();
        
        // Factory for ITestHardware since it's initialized later
        services.AddSingleton<ITestHardware>(sp => _testHardware ?? throw new InvalidOperationException("Hardware not initialized"));
        services.AddSingleton<IHardwareInfo>(sp => sp.GetRequiredService<ITestHardware>().HardwareInfo);
        services.AddSingleton<IShellCommandRunner>(sp => ShellCommandRunnerFactory.Create());
        services.AddSingleton<ICommunicationFactory, CommunicationFactory>();

        
        // ViewModels
        services.AddSingleton<TestHardwareRelayChannelsViewModel>();
        services.AddSingleton<TestStepConfiguratorViewModel>();
        services.AddSingleton<DeviceManagerViewModel>();
        services.AddSingleton<ProjectSettingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<TestingTabViewModel>();
        services.AddSingleton<ConfigTabViewModel>();
        services.AddSingleton<ScriptsManagerViewModel>();
        services.AddSingleton<AboutTabViewModel>();
        services.AddSingleton<HardwareTabViewModel>();
        services.AddSingleton<TestHardwareInfoViewModel>();
        services.AddSingleton<ScriptSelectorViewModel>();
        services.AddSingleton<CommandEditorViewModel>();
        services.AddSingleton<TestHardwareConnectWindowViewModel>();
        services.AddSingleton<ShellCommandEditorViewModel>();
        services.AddSingleton<ResponseMaskEditorViewModel>();

        // Windows
        services.AddTransient<MainWindow>();
        services.AddTransient<TestHardwareConnectWindow>();
        
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