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

namespace ATLab;

public partial class App : Application
{
    public static SettingsService SettingsService { get; private set; } = null!;

    public static bool SimulationMode { get; set; }

    private IUniversalTestHardwareInterface? _testHardware;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        SettingsService = new SettingsService();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {

            bool initSuccess = false;
            bool openConnectWindow = false;
            SerialPortService? service = null;

            service = new SerialPortService(SettingsService.Settings.LastComPort!);
            var openResult = service.TryOpen();
            if (!openResult.IsSuccess)
            {
                openConnectWindow = true;
                //await ShowInitErrorAndPromptUser(openResult.ErrorMessage, desktop);
            }
            else
            {
                _testHardware = new CTIAController(service);
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
                service?.Dispose();
                var serialPortWindow = new SerialPortConnectWindow();
                var tcs = new TaskCompletionSource<bool?>();

                if (serialPortWindow.DataContext is SerialPortConnectWindowViewModel vm)
                {
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

                        if (result == true)
                        {
                            _testHardware = vm._device;
                            SimulationMode = false;
                        }
                        else
                        {
                            _testHardware = new CTIAController(new SimulationService());
                            SimulationMode = true;
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

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(_testHardware!),
            };
            desktop.MainWindow.Show();
        }
        
        base.OnFrameworkInitializationCompleted();
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