using CommunityToolkit.Mvvm.Input;
using System.IO.Ports;
using System.Collections.ObjectModel;
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ATLab.Models;
using ATLab.Services;

namespace ATLab.ViewModels;

public partial class SerialPortConnectWindowViewModel : ViewModelBase
{
    public CTIAController? _cTIA;

    [ObservableProperty]
    private string _selectedPort = "";

    public ObservableCollection<string> AvailablePorts { get; } = new();

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private ConnectionStatus _status = ConnectionStatus.Disconnected;

    public event Action<bool>? Connected;

    public event Action? RequestClose;

    public SerialPortConnectWindowViewModel()
    {
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

        if (AvailablePorts.Count > 0)
            SelectedPort = AvailablePorts[0];
        else
            SelectedPort = string.Empty;

        ConnectCommand.NotifyCanExecuteChanged();
    }

    private bool CanConnect => !string.IsNullOrWhiteSpace(SelectedPort) && (_cTIA == null || App.SettingsService.Settings.LastComPort != SelectedPort);
    

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task<bool> Connect()
    {
        if (!CanConnect) return false;

        var service = new SerialPortService(SelectedPort!);
        var openResult = service.TryOpen();
        if (!openResult.IsSuccess)
        {
            StatusText = $"Failed to connect to {SelectedPort}";
            Status = ConnectionStatus.Failed;
            ConnectCommand.NotifyCanExecuteChanged();
            return false;
        }
        else
        {
            _cTIA = new CTIAController(service);
            var initResult = await _cTIA.InitializeAsync();
            if (!initResult.IsSuccess)
            {
                StatusText = $"Initialization failed: {initResult.ErrorMessage}";
                Status = ConnectionStatus.Failed;
                ConnectCommand.NotifyCanExecuteChanged();
                return false;
            }
            else{
                StatusText = $"Connected to {SelectedPort}";
                Status = ConnectionStatus.Connected;
                App.SettingsService.Settings.LastComPort = SelectedPort;
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
