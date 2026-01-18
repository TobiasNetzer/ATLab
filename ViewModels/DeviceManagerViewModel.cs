using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ivi.Visa;
using NationalInstruments.Visa;

namespace ATLab.ViewModels;

public partial class DeviceManagerViewModel : ViewModelBase
{
    private readonly IErrorService _errorService;

    [ObservableProperty]
    private ObservableCollection<Device> _devices = new();

    [ObservableProperty]
    private Device? _selectedDevice;

    public ObservableCollection<string> AvailableSerialPorts { get; } = new();
    public ObservableCollection<string> AvailableVisaResources { get; } = new();

    public ObservableCollection<DeviceType> AvailableDeviceTypes { get; } =
        new(Enum.GetValues<DeviceType>());

    public ObservableCollection<int> AvailableBaudRates { get; } =
        new() { 9600, 19200, 38400, 57600, 115200, 230400 };

    public ObservableCollection<int> AvailableDataBits { get; } =
        new() { 5, 6, 7, 8 };

    public ObservableCollection<Parity> AvailableParities { get; } =
        new(Enum.GetValues<Parity>());

    public ObservableCollection<StopBits> AvailableStopBits { get; } =
        new(Enum.GetValues<StopBits>());

    public ObservableCollection<Handshake> AvailableHandshakes { get; } =
        new(Enum.GetValues<Handshake>());

    public ObservableCollection<VisaTerminationMode> AvailableVisaTerminations { get; } =
        new(Enum.GetValues<VisaTerminationMode>());

    public DeviceManagerViewModel(IErrorService errorService)
    {
        _errorService = errorService;
    }
    
    [RelayCommand]
    private void RefreshResources()
    {
        if (SelectedDevice?.Type == DeviceType.SERIAL)
            RefreshSerialPorts();
        else
            RefreshVisaResources();
    }

    private void RefreshSerialPorts()
    {
        AvailableSerialPorts.Clear();

        foreach (var port in SerialPort.GetPortNames())
            AvailableSerialPorts.Add(port);

        if (SelectedDevice != null &&
            string.IsNullOrWhiteSpace(SelectedDevice.ResourceString) &&
            AvailableSerialPorts.Count > 0)
        {
            SelectedDevice.ResourceString = AvailableSerialPorts[0];
        }
    }

    private void RefreshVisaResources()
    {
        AvailableVisaResources.Clear();

        try
        {
            using var rm = new ResourceManager();
            foreach (var res in rm.Find("(USB|GPIB|ASRL|TCPIP)?*"))
                AvailableVisaResources.Add(res);
        }
        catch (DllNotFoundException)
        {
            _errorService.AddError("No VISA backend was detected on this system. Please install NI‑VISA or Keysight VISA.");
        }
        catch (TypeInitializationException)
        {
            _errorService.AddError("The VISA backend could not be initialized. It may be missing or corrupted.");
        }
        catch (VisaException)
        {
            _errorService.AddError("VISA is installed, but no resources could be enumerated.");
        }
        catch (Exception ex)
        {
            _errorService.AddError($"An unexpected error occurred while accessing VISA:\n{ex.Message}");
        }

        if (SelectedDevice != null &&
            string.IsNullOrWhiteSpace(SelectedDevice.ResourceString) &&
            AvailableVisaResources.Count > 0)
        {
            SelectedDevice.ResourceString = AvailableVisaResources[0];
        }
    }

    partial void OnSelectedDeviceChanged(Device? oldValue, Device? newValue)
    {
        RefreshResources();
    }
    
    [RelayCommand]
    private void AddDevice()
    {
        var newDevice = new Device
        {
            Name = "New Device",
            Type = DeviceType.SERIAL,
            ResourceString = string.Empty,

            Configuration = new DeviceConfiguration
            {
                BaudRate = 115200,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                VisaTimeoutMs = 2000,
                VisaTerminationMode = VisaTerminationMode.LF
            }
        };

        Devices.Add(newDevice);
        SelectedDevice = newDevice;
    }

    [RelayCommand]
    private void RemoveDevice()
    {
        if (SelectedDevice is null)
            return;

        Devices.Remove(SelectedDevice);
        SelectedDevice = null;
    }
}
