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

    [ObservableProperty]
    private string _editorDeviceName = string.Empty;
    
    [ObservableProperty]
    private DeviceType _editorDeviceType = DeviceType.SERIAL;
    
    [ObservableProperty]
    private string _editorResource = string.Empty;
    
    [ObservableProperty]
    private int _editorBaudRate = 115200;
    
    [ObservableProperty]
    private int _editorDataBits = 8;
    
    [ObservableProperty]
    private Parity _editorParity = Parity.None;
    
    [ObservableProperty]
    private StopBits _editorStopBits = StopBits.One;
    
    [ObservableProperty]
    private Handshake _editorHandshake = Handshake.None;
    
    [ObservableProperty] 
    private int _editorVisaTimeout = 2000;
    
    [ObservableProperty] 
    private VisaTerminationMode _editorVisaTermination = VisaTerminationMode.LF;
    
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
        RefreshResources();
    }
    
    [RelayCommand]
    private void RefreshResources()
    {
        if (EditorDeviceType == DeviceType.SERIAL)
            RefreshSerialPorts();
        else
            RefreshVisaResources();
    }

    private void RefreshSerialPorts()
    {
        AvailableSerialPorts.Clear();

        foreach (var port in SerialPort.GetPortNames())
            AvailableSerialPorts.Add(port);

        if (string.IsNullOrEmpty(EditorResource) && AvailableSerialPorts.Count > 0)
            EditorResource = AvailableSerialPorts[0];
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
        
        if (string.IsNullOrEmpty(EditorResource) && AvailableVisaResources.Count > 0)
            EditorResource = AvailableVisaResources[0];
    }

    partial void OnEditorDeviceTypeChanged(DeviceType oldValue, DeviceType newValue)
    {
        EditorResource = string.Empty;
        RefreshResources();
    }
    
    private byte VisaTerminationToByte(VisaTerminationMode mode)
    {
        return mode switch
        {
            VisaTerminationMode.LF   => (byte)10,
            VisaTerminationMode.CR   => (byte)13,
            VisaTerminationMode.NONE => (byte)0,

            // CRLF: VISA uses LF as termination, CR must be written manually
            VisaTerminationMode.CRLF => (byte)10,

            _ => (byte)10
        };
    }

    [RelayCommand]
    private void AddDevice()
    {
        if (string.IsNullOrWhiteSpace(EditorDeviceName) ||
            string.IsNullOrWhiteSpace(EditorResource))
            return;

        var config = new DeviceConfiguration
        {
            BaudRate = EditorBaudRate,
            DataBits = EditorDataBits,
            Parity = EditorParity,
            StopBits = EditorStopBits,
            Handshake = EditorHandshake,
            VisaTimeoutMs = EditorVisaTimeout,
            VisaTerminationChar = VisaTerminationToByte(EditorVisaTermination)
        };

        Devices.Add(new Device
        {
            Name = EditorDeviceName,
            ResourceString = EditorResource,
            Type = EditorDeviceType,
            Configuration = config
        });

        ResetEditor();
    }

    [RelayCommand]
    private void EditDevice()
    {
        if (SelectedDevice is null)
            return;

        //SelectedDevice.Name = EditorDeviceName; // Don't update Name since it's what binds the Device to TestStep
        SelectedDevice.ResourceString = EditorResource;
        SelectedDevice.Type = EditorDeviceType;

        SelectedDevice.Configuration.BaudRate = EditorBaudRate;
        SelectedDevice.Configuration.DataBits = EditorDataBits;
        SelectedDevice.Configuration.Parity = EditorParity;
        SelectedDevice.Configuration.StopBits = EditorStopBits;
        SelectedDevice.Configuration.Handshake = EditorHandshake;

        SelectedDevice.Configuration.VisaTimeoutMs = EditorVisaTimeout;
        SelectedDevice.Configuration.VisaTerminationChar = VisaTerminationToByte(EditorVisaTermination);

    }

    [RelayCommand]
    private void RemoveDevice()
    {
        if (SelectedDevice is null) return;
        Devices.Remove(SelectedDevice);
    }


    private void ResetEditor()
    {
        EditorDeviceName = string.Empty;
        EditorResource = string.Empty;
        EditorDeviceType = DeviceType.SERIAL;

        EditorBaudRate = 115200;
        EditorDataBits = 8;
        EditorParity = Parity.None;
        EditorStopBits = StopBits.One;
        EditorHandshake = Handshake.None;

        EditorVisaTimeout = 2000;
        EditorVisaTermination = VisaTerminationMode.LF;
    }
}
