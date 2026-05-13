using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO.Ports;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NationalInstruments.Visa;

namespace ATLab.ViewModels;

public partial class DeviceManagerViewModel : ViewModelBase
{
    private readonly IErrorService _errorService;

    [ObservableProperty]
    private ObservableCollection<Device> _devices = new();

    [ObservableProperty]
    private Device? _selectedDevice;
    
    [ObservableProperty]
    private string _framingTimeoutMsString = "100";
    
    [ObservableProperty]
    private string _visaTimeoutMsString = "2000";

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
    
    public ObservableCollection<SerialTerminationMode> AvailableSerialTermination { get; } =
        new(Enum.GetValues<SerialTerminationMode>());
    
    public ObservableCollection<MessageFramingMode> AvailableFramingModes { get; } =
        new(Enum.GetValues<MessageFramingMode>());

    public ObservableCollection<VisaTerminationMode> AvailableVisaTerminations { get; } =
        new(Enum.GetValues<VisaTerminationMode>());

    public DeviceManagerViewModel(IErrorService errorService)
    {
        _errorService = errorService;
    }
    
    [RelayCommand]
    private void RefreshResources()
    {
        switch (SelectedDevice?.Type)
        {
            case DeviceType.SERIAL: RefreshSerialPorts();
                break;
            case DeviceType.VISA: RefreshVisaResources();
                break;
            case null:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
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
        catch (Exception ex)
        {
            _errorService.AddError($"An unexpected error occurred while accessing VISA: {ex.Message}");
        }

        if (SelectedDevice != null &&
            string.IsNullOrWhiteSpace(SelectedDevice.ResourceString) &&
            AvailableVisaResources.Count > 0)
        {
            SelectedDevice.ResourceString = AvailableVisaResources[0];
        }
    }
    
    partial void OnSelectedDeviceChanged(Device? value)
    {
        if (value == null)
            return;
        
        FramingTimeoutMsString = SelectedDevice?.Configuration.FramingTimeoutMs.ToString() ?? "";
        VisaTimeoutMsString = SelectedDevice?.Configuration.VisaTimeoutMs.ToString() ?? "";
    }

    partial void OnFramingTimeoutMsStringChanged(string? oldValue, string newValue)
    {
        if (oldValue == newValue)
            return;

        if (SelectedDevice == null)
            return;
        
        if (string.IsNullOrWhiteSpace(newValue))
            return;
        
        SelectedDevice.Configuration.FramingTimeoutMs = (int) double.Parse(newValue, CultureInfo.CurrentCulture);
    }

    partial void OnVisaTimeoutMsStringChanged(string? oldValue, string newValue)
    {
        if (oldValue == newValue)
            return;

        if (SelectedDevice == null)
            return;
        
        if (string.IsNullOrWhiteSpace(newValue))
            return;
        
        SelectedDevice.Configuration.VisaTimeoutMs = (int) double.Parse(newValue, CultureInfo.CurrentCulture);
    }

    [RelayCommand]
    private void AddDevice()
    {
        var newDevice = new Device
        {
            Name = "New Device",
            Type = DeviceType.SERIAL,
            ResourceString = string.Empty,

            Configuration = new DeviceConfiguration()
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