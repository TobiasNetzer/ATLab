using System.Collections.ObjectModel;
using System.IO.Ports;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class DeviceManagerViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Device> _serialDevices = new();
    
    [ObservableProperty]
    private Device? _selectedDevice;

    [ObservableProperty]
    private string _customDeviceName = string.Empty;

    [ObservableProperty]
    private string _selectedPort = string.Empty;

    public ObservableCollection<string> AvailablePorts { get; } = new();

    public DeviceManagerViewModel()
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

        SerialDevices.Add(new Device
        {
            Name = CustomDeviceName,
            ResourceString = SelectedPort
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