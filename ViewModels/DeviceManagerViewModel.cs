using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using ATLab.Enums;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NationalInstruments.Visa;

namespace ATLab.ViewModels;

public partial class DeviceManagerViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Device> _devices = new();

    [ObservableProperty]
    private Device? _selectedDevice;

    [ObservableProperty]
    private string _customDeviceName = string.Empty;

    [ObservableProperty]
    private string _selectedResource = string.Empty;

    [ObservableProperty]
    private DeviceType _selectedDeviceType = DeviceType.SERIAL;

    public ObservableCollection<string> AvailableSerialPorts { get; } = new();
    public ObservableCollection<string> AvailableVisaResources { get; } = new();
    public ObservableCollection<DeviceType> AvailableDeviceTypes { get; } =
        new(Enum.GetValues<DeviceType>());

    public DeviceManagerViewModel()
    {
        RefreshResources();
    }

    [RelayCommand]
    private void RefreshResources()
    {
        if (SelectedDeviceType == DeviceType.SERIAL)
            RefreshSerialPorts();
        else if (SelectedDeviceType == DeviceType.VISA)
            RefreshVisaResources();
    }

    private void RefreshSerialPorts()
    {
        AvailableSerialPorts.Clear();

        foreach (var port in SerialPort.GetPortNames())
            AvailableSerialPorts.Add(port);

        if (string.IsNullOrEmpty(SelectedResource) && AvailableSerialPorts.Count > 0)
            SelectedResource = AvailableSerialPorts[0];
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
        catch
        {
            // VISA not installed or no resources found
        }

        if (string.IsNullOrEmpty(SelectedResource) && AvailableVisaResources.Count > 0)
            SelectedResource = AvailableVisaResources[0];
    }

    partial void OnSelectedDeviceTypeChanged(DeviceType oldValue, DeviceType newValue)
    {
        SelectedResource = string.Empty;
        RefreshResources();
    }

    [RelayCommand]
    private void AddDevice()
    {
        if (string.IsNullOrWhiteSpace(CustomDeviceName) ||
            string.IsNullOrWhiteSpace(SelectedResource))
            return;

        Devices.Add(new Device
        {
            Name = CustomDeviceName,
            ResourceString = SelectedResource,
            Type = SelectedDeviceType
        });

        CustomDeviceName = string.Empty;
    }

    [RelayCommand]
    private void RemoveDevice()
    {
        if (SelectedDevice is null) return;
        Devices.Remove(SelectedDevice);
    }

    [RelayCommand]
    private void EditDevice()
    {
        if (SelectedDevice is null ||
            string.IsNullOrWhiteSpace(SelectedResource))
            return;

        SelectedDevice.ResourceString = SelectedResource;
        SelectedDevice.Type = SelectedDeviceType;

        CustomDeviceName = string.Empty;
    }
}
