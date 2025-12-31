using System.Collections.ObjectModel;
using System.IO.Ports;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class SerialDeviceManagerViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<SerialDevices> _serialDevices = new();
    
    [ObservableProperty]
    private SerialDevices? _selectedDevice;

    [ObservableProperty]
    private string _customDeviceName = string.Empty;

    [ObservableProperty]
    private string _selectedPort = string.Empty;

    public ObservableCollection<string> AvailablePorts { get; } = new();

    public SerialDeviceManagerViewModel()
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
        
        if (string.IsNullOrEmpty(SelectedPort) && AvailablePorts.Count > 0)
        {
            SelectedPort = AvailablePorts[0];
        }
    }

    [RelayCommand]
    private void AddDevice()
    {
        if (string.IsNullOrWhiteSpace(CustomDeviceName) || string.IsNullOrWhiteSpace(SelectedPort))
            return;

        SerialDevices.Add(new SerialDevices
        {
            Name = CustomDeviceName,
            SerialPort = SelectedPort
        });

        CustomDeviceName = string.Empty;
    }

    [RelayCommand]
    private void RemoveDevice()
    {
        if (SelectedDevice is null) return;
        SerialDevices.Remove(SelectedDevice);
    }

    [RelayCommand]
    private void EditPort()
    {
        if(SelectedDevice is null || string.IsNullOrWhiteSpace(SelectedPort)) return;
        CustomDeviceName = SelectedDevice?.Name ?? string.Empty;
        RemoveDevice();
        AddDevice();
    }
}