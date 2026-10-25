using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using ATLab.BoundaryScan.Bsdl;
using ATLab.Enums;
using ATLab.Helpers;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.ViewModels;

/// <summary>
/// Runtime state of one chain device: parsed BSDL, loaded instruction, boundary register content and pins.
/// </summary>
public partial class BoundaryScanDeviceViewModel : ObservableObject
{
    private bool[] _outputs = [];
    private bool[]? _captured;
    private string _pinFilter = string.Empty;
    private bool _showLinkagePins = true;

    public BoundaryScanDeviceConfig Config { get; }

    /// <summary>Raised after an output cell was changed by the user.</summary>
    public event Action<BoundaryScanDeviceViewModel>? OutputsChanged;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    private int _position;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName), nameof(HasBsdl), nameof(EntityName), nameof(IdCodeMatches),
        nameof(InstructionLengthText), nameof(BoundaryLengthText), nameof(IsIrLengthEditable))]
    private BsdlDevice? _bsdl;

    [ObservableProperty]
    private string _bsdlError = string.Empty;

    /// <summary>Instruction currently loaded in the device, null while not connected.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBypassActive), nameof(IsSampleActive), nameof(IsExtestActive), nameof(ActiveInstructionText))]
    private BoundaryScanInstruction? _activeInstruction;

    /// <summary>Incremented whenever pin states change, lets the package view redraw.</summary>
    [ObservableProperty]
    private int _stateVersion;

    public ObservableCollection<string> Packages { get; } = new();
    public ObservableCollection<BoundaryScanPinViewModel> Pins { get; } = new();
    public ObservableCollection<BoundaryScanPinViewModel> VisiblePins { get; } = new();

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(Config.Name) ? Config.Name
        : Bsdl != null ? Bsdl.EntityName
        : $"Device {Position}";

    /// <summary>
    /// Package selection of the view. The ComboBox clears its selection while the selected device changes,
    /// empty values are ignored so that does not wipe the package of the device.
    /// </summary>
    public string? SelectedPackage
    {
        get => Config.PackageName;
        set
        {
            if (!string.IsNullOrEmpty(value))
                Config.PackageName = value;
        }
    }

    public bool HasBsdl => Bsdl != null;
    public string EntityName => Bsdl?.EntityName ?? string.Empty;
    public bool IsIrLengthEditable => Bsdl == null;

    public string IdCodeText => string.IsNullOrWhiteSpace(Config.IdCode) ? "—" : $"0x{Config.IdCode}";

    /// <summary>Null when there is nothing to compare.</summary>
    public bool? IdCodeMatches =>
        Bsdl?.IdCodePattern == null || !TryGetIdCode(out var idCode) ? null : Bsdl.MatchesIdCode(idCode);

    public string InstructionLengthText => Config.IrLength > 0 ? Config.IrLength.ToString() : "?";
    public string BoundaryLengthText => Bsdl?.BoundaryLength.ToString() ?? "—";

    public bool IsBypassActive => ActiveInstruction == BoundaryScanInstruction.BYPASS;
    public bool IsSampleActive => ActiveInstruction == BoundaryScanInstruction.SAMPLE;
    public bool IsExtestActive => ActiveInstruction == BoundaryScanInstruction.EXTEST;
    public string ActiveInstructionText => ActiveInstruction?.ToString() ?? string.Empty;

    public BoundaryScanDeviceViewModel(BoundaryScanDeviceConfig config, int position)
    {
        Config = config;
        Position = position;

        Config.PropertyChanged += ConfigChanged;
        LoadBsdl();
    }

    public void Detach()
    {
        Config.PropertyChanged -= ConfigChanged;
    }

    public BoundaryScanChainEntry ToChainEntry(BoundaryScanInstruction instruction) =>
        new(Config.IrLength, Bsdl, instruction, ActiveInstruction, (bool[])_outputs.Clone());

    public void ApplyCapture(bool[]? captured)
    {
        _captured = captured;

        foreach (var pin in Pins)
        {
            pin.InputValue = captured != null && pin.InputCell is { } cell && cell < captured.Length
                ? captured[cell]
                : null;
        }

        StateVersion++;
    }

    public void SetActiveInstruction(BoundaryScanInstruction? instruction)
    {
        ActiveInstruction = instruction;

        foreach (var pin in Pins)
            pin.IsDriveEnabled = instruction == BoundaryScanInstruction.EXTEST;

        if (instruction is null or BoundaryScanInstruction.BYPASS)
            ApplyCapture(null);
    }

    public void ResetOutputsToSafeValues()
    {
        _outputs = Bsdl?.CreateSafeOutputs() ?? [];

        UpdateDriveStates();
    }

    /// <summary>Pins whose output or output enable differs from the BSDL safe values.</summary>
    public IEnumerable<BoundaryScanPinViewModel> GetChangedOutputPins()
    {
        if (Bsdl == null)
            return [];

        var safe = Bsdl.CreateSafeOutputs();

        return Pins.Where(p => p.CanDrive &&
                               (p.OutputCell is { } output && safe[output] != _outputs[output] ||
                                p.ControlCell is { } control && safe[control] != _outputs[control]));
    }

    public void ApplyPinFilter(string filter, bool showLinkagePins)
    {
        _pinFilter = filter.Trim();
        _showLinkagePins = showLinkagePins;

        VisiblePins.Clear();

        foreach (var pin in Pins.Where(IsPinVisible))
            VisiblePins.Add(pin);
    }

    public bool TryGetIdCode(out uint idCode)
    {
        return uint.TryParse(Config.IdCode, System.Globalization.NumberStyles.HexNumber, null, out idCode);
    }

    public void LoadBsdl()
    {
        BsdlError = string.Empty;
        _captured = null;

        if (string.IsNullOrWhiteSpace(Config.BsdlFilePath))
        {
            Bsdl = null;
        }
        else if (!File.Exists(Config.BsdlFilePath))
        {
            Bsdl = null;
            BsdlError = $"File not found: {Config.BsdlFilePath}";
        }
        else
        {
            var result = BsdlParser.ParseFile(Config.BsdlFilePath);
            Bsdl = result.Value;
            BsdlError = result.ErrorMessage;
        }

        Packages.Clear();

        if (Bsdl != null)
        {
            foreach (var package in Bsdl.Packages.Keys.OrderBy(p => p))
                Packages.Add(package);

            Config.IrLength = Bsdl.InstructionLength;

            if (!Bsdl.Packages.ContainsKey(Config.PackageName ?? string.Empty))
            {
                Config.PackageName = Bsdl.Packages.ContainsKey(Bsdl.DefaultPackage)
                    ? Bsdl.DefaultPackage
                    : Packages.FirstOrDefault() ?? string.Empty;
            }
        }

        ResetOutputsToSafeValues();
        BuildPins();
        OnPropertyChanged(nameof(IdCodeMatches));
    }

    private void BuildPins()
    {
        Pins.Clear();

        if (Bsdl != null)
        {
            var directions = Bsdl.Ports.ToDictionary(p => p.Name, p => p.Direction, StringComparer.OrdinalIgnoreCase);

            // Without a pin map the ports are listed on their own
            var pins = Bsdl.Packages.TryGetValue(Config.PackageName ?? string.Empty, out var packagePins)
                ? packagePins
                : Bsdl.Ports.Select(p => new BsdlPin(p.Name, p.Name)).ToList();

            foreach (var pin in pins.OrderBy(p => p.PinName, NaturalStringComparer.Instance))
            {
                Pins.Add(new BoundaryScanPinViewModel(
                    pin.PinName,
                    pin.Port,
                    directions.GetValueOrDefault(pin.Port, BsdlPortDirection.Linkage),
                    Bsdl.TapPorts.Contains(pin.Port),
                    Bsdl.GetPortCells(pin.Port).Input,
                    Bsdl.GetPortCells(pin.Port).Output,
                    Config.GetPinLabel(pin.Port),
                    DrivePin,
                    p => Config.SetPinLabel(p.Port, p.Label))
                {
                    IsDriveEnabled = ActiveInstruction == BoundaryScanInstruction.EXTEST
                });
            }

            foreach (var group in Pins.Where(p => p.CanTristate).GroupBy(p => p.ControlCell))
            {
                foreach (var pin in group)
                    pin.SharedControlPorts = group.Where(p => p != pin).Select(p => p.Port).ToList();
            }
        }

        UpdateDriveStates();
        ApplyCapture(_captured);
        ApplyPinFilter(_pinFilter, _showLinkagePins);
    }

    private void DrivePin(BoundaryScanPinViewModel pin, PinDriveState state)
    {
        if (Bsdl?.GetPortCells(pin.Port).Output is not { } outputCell ||
            !BsdlDevice.ApplyDrive(_outputs, outputCell, state))
            return;

        // Control cells are often shared by several outputs, all of them are updated
        UpdateDriveStates();
        OutputsChanged?.Invoke(this);
    }

    private void UpdateDriveStates()
    {
        foreach (var pin in Pins)
        {
            if (pin.OutputCell is not { } outputCell || outputCell >= _outputs.Length)
            {
                pin.DriveState = null;
                continue;
            }

            if (pin.ControlCell is { } controlCell && controlCell < _outputs.Length &&
                _outputs[controlCell] == pin.ControlDisableValue)
            {
                pin.DriveState = PinDriveState.HIGH_Z;
            }
            else
            {
                pin.DriveState = _outputs[outputCell] ? PinDriveState.HIGH : PinDriveState.LOW;
            }
        }

        StateVersion++;
    }

    private bool IsPinVisible(BoundaryScanPinViewModel pin)
    {
        if (!_showLinkagePins && pin.IsLinkage)
            return false;

        return _pinFilter.Length == 0 ||
               pin.PinName.Contains(_pinFilter, StringComparison.OrdinalIgnoreCase) ||
               pin.Port.Contains(_pinFilter, StringComparison.OrdinalIgnoreCase) ||
               pin.Label.Contains(_pinFilter, StringComparison.OrdinalIgnoreCase);
    }

    private void ConfigChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(BoundaryScanDeviceConfig.BsdlFilePath):
                LoadBsdl();
                break;
            case nameof(BoundaryScanDeviceConfig.PackageName):
                OnPropertyChanged(nameof(SelectedPackage));
                BuildPins();
                break;
            case nameof(BoundaryScanDeviceConfig.Name):
                OnPropertyChanged(nameof(DisplayName));
                break;
            case nameof(BoundaryScanDeviceConfig.IdCode):
                OnPropertyChanged(nameof(IdCodeText));
                OnPropertyChanged(nameof(IdCodeMatches));
                break;
            case nameof(BoundaryScanDeviceConfig.IrLength):
                OnPropertyChanged(nameof(InstructionLengthText));
                break;
        }
    }
}