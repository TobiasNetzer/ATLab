using CommunityToolkit.Mvvm.Input;
using System.IO.Ports;
using System.Collections.ObjectModel;
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ATLab.Enums;
using ATLab.CTIA;
using ATLab.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ATLab.ViewModels;

public partial class SerialPortConnectWindowViewModel : ViewModelBase
{
    public ITestHardware? TestHardware;

    [ObservableProperty]
    private string _selectedPort = "";

    public ObservableCollection<string> AvailablePorts { get; } = new();

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private ConnectionStatus _status = ConnectionStatus.DISCONNECTED;

    public event Action<bool>? Connected;

    public event Action? RequestClose;

    private readonly ISettingsService _settingsService;
    private readonly IServiceProvider _serviceProvider;

    public SerialPortConnectWindowViewModel(ISettingsService settingsService, IServiceProvider serviceProvider)
    {
        _settingsService = settingsService;
        _serviceProvider = serviceProvider;
        RefreshPorts();
    }

    [RelayCommand]
    private void RefreshPorts()
    {
        AvailablePorts.Clear();

        foreach (var port in SerialPort.GetPortNames())
        {
            AvailablePorts.Add(port);
        }

        SelectedPort = AvailablePorts.Count > 0 ? AvailablePorts[0] : string.Empty;

        ConnectCommand.NotifyCanExecuteChanged();
    }

    private bool CanConnect => !string.IsNullOrWhiteSpace(SelectedPort) && (TestHardware == null || _settingsService.Settings.LastComPort != SelectedPort);
    

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task<bool> Connect()
    {
        if (!CanConnect) return false;

        var testHardwareInterfaceFactory = _serviceProvider.GetRequiredService<ICommunicationFactory>();
        var testHardwareInterface = testHardwareInterfaceFactory.CreateSerial(SelectedPort);
        var openResult = await testHardwareInterface.ConnectAsync();
        
        if (!openResult.IsSuccess)
        {
            StatusText = $"Failed to connect to {SelectedPort}";
            Status = ConnectionStatus.FAILED;
            ConnectCommand.NotifyCanExecuteChanged();
            return false;
        }
        else
        {
            var communication = ActivatorUtilities.CreateInstance<CtiaCommunication>(_serviceProvider, testHardwareInterface);
            TestHardware = ActivatorUtilities.CreateInstance<CtiaHardware>(_serviceProvider, communication);
            var initResult = await TestHardware.InitializeAsync();
            if (!initResult.IsSuccess)
            {
                StatusText = $"Initialization failed: {initResult.ErrorMessage}";
                Status = ConnectionStatus.FAILED;
                ConnectCommand.NotifyCanExecuteChanged();
                return false;
            }
            else{
                StatusText = $"Connected to {SelectedPort}";
                Status = ConnectionStatus.CONNECTED;
                _settingsService.Settings.LastComPort = SelectedPort;
                Connected?.Invoke(true);
                ConnectCommand.NotifyCanExecuteChanged();
                return true;
            }
        }
    }

    [RelayCommand]
    private void SimulationMode()
    {
        Connected?.Invoke(false);
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }
}
