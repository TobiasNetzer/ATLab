using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using ATLab.BoundaryScan.Buses;
using ATLab.Enums;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

/// <summary>One signal of the selected bus with its device and pin pickers.</summary>
public partial class BoundaryScanBusPinRowViewModel : ObservableObject
{
    private readonly ObservableCollection<BoundaryScanDeviceViewModel> _devices;

    public BoundaryScanBusPin Model { get; }

    public string SignalName => Model.SignalName;

    public ObservableCollection<BoundaryScanDeviceViewModel> Devices => _devices;

    /// <summary>Pins of the chosen device that can take part in a bus.</summary>
    public IEnumerable<BoundaryScanPinViewModel> AvailablePins =>
        Device?.Pins.Where(p => !p.IsTapPin && (p.CanDrive || p.CanRead)) ?? [];

    public BoundaryScanDeviceViewModel? Device
    {
        get => _devices.FirstOrDefault(d => d.Config.Id == Model.DeviceId);
        set
        {
            var id = value?.Config.Id ?? string.Empty;
            if (id == Model.DeviceId)
                return;

            Model.DeviceId = id;
            Model.Port = string.Empty;

            OnPropertyChanged();
            OnPropertyChanged(nameof(AvailablePins));
            OnPropertyChanged(nameof(Pin));
            OnPropertyChanged(nameof(DeviceText));
            OnPropertyChanged(nameof(PinText));
        }
    }

    public BoundaryScanPinViewModel? Pin
    {
        get => AvailablePins.FirstOrDefault(p => string.Equals(p.Port, Model.Port, StringComparison.OrdinalIgnoreCase));
        set
        {
            // The ComboBox clears its selection while rows are rebuilt, that must not remove the assignment
            if (value == null)
                return;

            Model.Port = value.Port;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PinText));
        }
    }

    public string DeviceText => Device?.DisplayName ?? (Model.DeviceId.Length > 0 ? "?" : string.Empty);

    public string PinText => Pin?.DisplayText ?? Model.Port;

    public BoundaryScanBusPinRowViewModel(BoundaryScanBusPin model, ObservableCollection<BoundaryScanDeviceViewModel> devices)
    {
        Model = model;
        _devices = devices;
    }
}

/// <summary>Bus setup of the Boundary Scan tab. Buses are only defined here, test steps select them.</summary>
public partial class BoundaryScanBusEditorViewModel : ObservableObject
{
    private readonly BoundaryScanConfiguration _configuration;
    private readonly ObservableCollection<BoundaryScanDeviceViewModel> _devices;

    public ObservableCollection<BoundaryScanBus> Buses => _configuration.Buses;

    public List<BoundaryScanBusType> AvailableTypes { get; } = new(Enum.GetValues<BoundaryScanBusType>());

    public List<int> SpiModes { get; } = [0, 1, 2, 3];

    public ObservableCollection<BoundaryScanBusPinRowViewModel> PinRows { get; } = new();

    public ObservableCollection<string> Problems { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedBus), nameof(IsSpi), nameof(IsPinGroup))]
    [NotifyCanExecuteChangedFor(nameof(RemoveBusCommand))]
    private BoundaryScanBus? _selectedBus;

    public bool HasSelectedBus => SelectedBus != null;
    public bool IsSpi => SelectedBus?.Type == BoundaryScanBusType.SPI;
    public bool IsPinGroup => SelectedBus?.Type == BoundaryScanBusType.PIN_GROUP;
    public bool IsBusValid => HasSelectedBus && Problems.Count == 0;

    public BoundaryScanBusEditorViewModel(BoundaryScanConfiguration configuration, ObservableCollection<BoundaryScanDeviceViewModel> devices)
    {
        _configuration = configuration;
        _devices = devices;

        Buses.CollectionChanged += BusesChanged;
        _devices.CollectionChanged += (_, _) => RebuildRows();
        _configuration.ConfigurationChanged += Validate;

        SelectedBus = Buses.FirstOrDefault();
    }

    [RelayCommand]
    private void AddBus()
    {
        var bus = new BoundaryScanBus { Name = $"Bus {Buses.Count + 1}" };
        bus.UpdateSignals();
        Buses.Add(bus);
        SelectedBus = bus;
    }

    private bool CanRemoveBus() => SelectedBus != null;

    [RelayCommand(CanExecute = nameof(CanRemoveBus))]
    private void RemoveBus()
    {
        if (SelectedBus != null)
            Buses.Remove(SelectedBus);
    }

    partial void OnSelectedBusChanged(BoundaryScanBus? oldValue, BoundaryScanBus? newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= SelectedBusPropertyChanged;

        if (newValue != null)
            newValue.PropertyChanged += SelectedBusPropertyChanged;

        RebuildRows();
    }

    private void SelectedBusPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(BoundaryScanBus.Type) or nameof(BoundaryScanBus.Width):
                SelectedBus!.UpdateSignals();
                OnPropertyChanged(nameof(IsSpi));
                OnPropertyChanged(nameof(IsPinGroup));
                break;
            case nameof(BoundaryScanBus.Pins):
                if (PinRows.Count != SelectedBus!.Pins.Count || PinRows.Where((r, i) => r.Model != SelectedBus.Pins[i]).Any())
                    RebuildRows();
                break;
        }
    }

    private void BusesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Loading a project replaces all buses
        if (SelectedBus == null || !Buses.Contains(SelectedBus))
            SelectedBus = Buses.FirstOrDefault();
    }

    private void RebuildRows()
    {
        PinRows.Clear();

        foreach (var pin in SelectedBus?.Pins ?? [])
            PinRows.Add(new BoundaryScanBusPinRowViewModel(pin, _devices));

        Validate();
    }

    private void Validate()
    {
        Problems.Clear();

        if (SelectedBus != null)
        {
            BusPinDevice? FindDevice(string id) =>
                _devices.FirstOrDefault(d => d.Config.Id == id) is { } d ? new BusPinDevice(d.DisplayName, d.Bsdl) : null;

            foreach (var problem in BoundaryScanBusValidator.Validate(SelectedBus, FindDevice))
                Problems.Add(problem);
        }

        OnPropertyChanged(nameof(IsBusValid));
    }
}