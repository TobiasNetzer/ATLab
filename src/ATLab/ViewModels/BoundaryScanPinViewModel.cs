using System;
using System.Collections.Generic;
using ATLab.BoundaryScan.Bsdl;
using ATLab.Enums;
using ATLab.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

/// <summary>
/// A pin of a boundary scan device with its cells from the BSDL file, the last read state and the state to drive.
/// </summary>
public partial class BoundaryScanPinViewModel : ObservableObject
{
    private readonly Action<BoundaryScanPinViewModel, PinDriveState> _drive;
    private readonly Action<BoundaryScanPinViewModel>? _labelChanged;

    public string PinName { get; }
    public string Port { get; }
    public BsdlPortDirection Direction { get; }
    public bool IsTapPin { get; }

    public int? InputCell { get; }
    public int? OutputCell { get; }
    public int? ControlCell { get; }
    public bool ControlDisableValue { get; }

    public bool IsLinkage => Direction == BsdlPortDirection.Linkage;
    public bool CanRead => InputCell != null;
    public bool CanDrive => OutputCell != null && !IsTapPin;
    public bool CanTristate => CanDrive && ControlCell != null;

    public string DirectionText => IsTapPin ? "TAP" : Direction.ToString().ToUpperInvariant();

    public string CellText
    {
        get
        {
            var parts = new List<string>();
            if (InputCell != null) parts.Add($"I{InputCell}");
            if (OutputCell != null) parts.Add($"O{OutputCell}");
            if (ControlCell != null) parts.Add($"C{ControlCell}");
            return string.Join(" ", parts);
        }
    }

    /// <summary>Ports whose outputs are enabled by the same control cell, switching this pin to Z affects them too.</summary>
    public IReadOnlyList<string> SharedControlPorts { get; set; } = [];

    public string? CellToolTip
    {
        get
        {
            if (CellText.Length == 0)
                return null;

            var text = string.Format(LocalizationService.Instance["ToolTip_BoundaryCells"], CellText);

            if (SharedControlPorts.Count > 0)
            {
                text += "\n\n" + string.Format(LocalizationService.Instance["ToolTip_SharedControlCell"],
                    ControlCell, string.Join(", ", SharedControlPorts));
            }

            return text;
        }
    }

    /// <summary>User defined signal name, e.g. the net name from the schematic.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private string _label;

    public string DisplayText => string.IsNullOrWhiteSpace(Label) ? Port : $"{Port} · {Label.Trim()}";

    /// <summary>Captured pin level, null while the device is not in SAMPLE or EXTEST.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InputText))]
    private bool? _inputValue;

    /// <summary>Requested output state, null for pins without output cell.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDrivenLow), nameof(IsDrivenHigh), nameof(IsHighZ))]
    private PinDriveState? _driveState;

    /// <summary>Outputs only take effect in EXTEST, editing is limited to that mode.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDriveEditable), nameof(IsTristateEditable))]
    private bool _isDriveEnabled;

    public bool IsDriveEditable => CanDrive && IsDriveEnabled;
    public bool IsTristateEditable => CanTristate && IsDriveEnabled;

    public bool IsDrivenLow => DriveState == PinDriveState.LOW;
    public bool IsDrivenHigh => DriveState == PinDriveState.HIGH;
    public bool IsHighZ => DriveState == PinDriveState.HIGH_Z;

    public string InputText => InputValue switch
    {
        true => "1",
        false => "0",
        null => string.Empty
    };

    public BoundaryScanPinViewModel(
        string pinName,
        string port,
        BsdlPortDirection direction,
        bool isTapPin,
        BsdlCell? inputCell,
        BsdlCell? outputCell,
        string label,
        Action<BoundaryScanPinViewModel, PinDriveState> drive,
        Action<BoundaryScanPinViewModel>? labelChanged = null)
    {
        _label = label;
        _labelChanged = labelChanged;
        PinName = pinName;
        Port = port;
        Direction = direction;
        IsTapPin = isTapPin;
        InputCell = inputCell?.Number;
        OutputCell = outputCell?.Number;
        ControlCell = outputCell?.ControlCell;
        ControlDisableValue = outputCell?.DisableValue ?? true;
        _drive = drive;
    }

    partial void OnLabelChanged(string value) => _labelChanged?.Invoke(this);

    [RelayCommand]
    private void Drive(PinDriveState state)
    {
        if (state == PinDriveState.HIGH_Z ? IsTristateEditable : IsDriveEditable)
            _drive(this, state);
    }

    /// <summary>Toggles between low and high, a disabled output is enabled driving high.</summary>
    public void Toggle()
    {
        Drive(DriveState == PinDriveState.HIGH ? PinDriveState.LOW : PinDriveState.HIGH);
    }
}