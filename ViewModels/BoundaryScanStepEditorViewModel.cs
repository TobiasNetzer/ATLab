using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

/// <summary>One row of the pin action table, resolves device and pin for display.</summary>
public class BoundaryScanPinActionViewModel
{
    public BoundaryScanPinAction Model { get; }
    public string DeviceName { get; }
    public string PinText { get; }

    /// <summary>False when the device was removed from the chain, the step fails until the row is fixed.</summary>
    public bool IsResolved { get; }

    public BoundaryScanPinActionType Action
    {
        get => Model.Action;
        set => Model.Action = value;
    }

    public BoundaryScanPinActionViewModel(BoundaryScanPinAction model, BoundaryScanDeviceViewModel? device)
    {
        Model = model;
        IsResolved = device != null;
        DeviceName = device?.DisplayName ?? "?";

        var label = device?.Config.GetPinLabel(model.Port) ?? string.Empty;
        PinText = label.Length > 0 ? $"{model.Port} · {label}" : model.Port;
    }
}

public partial class BoundaryScanStepEditorViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILoggingService _loggingService;
    private readonly BoundaryScanTabViewModel _boundaryScanTab;

    private ObservableCollection<BoundaryScanPinAction>? _observedActions;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private TestStep? _testStep;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveActionCommand))]
    private BoundaryScanPinActionViewModel? _selectedAction;

    [ObservableProperty]
    private BoundaryScanDeviceViewModel? _newDevice;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddActionCommand))]
    private BoundaryScanPinViewModel? _newPin;

    [ObservableProperty]
    private BoundaryScanPinActionType _newAction = BoundaryScanPinActionType.DRIVE_HIGH;

    public ObservableCollection<BoundaryScanPinActionViewModel> Actions { get; } = new();

    public ObservableCollection<BoundaryScanDeviceViewModel> Devices => _boundaryScanTab.Devices;

    public ObservableCollection<BoundaryScanPinViewModel> NewPins { get; } = new();

    public List<BoundaryScanPinActionType> AvailableActions { get; } = new(Enum.GetValues<BoundaryScanPinActionType>());

    public BoundaryScanStepEditorViewModel(
        ISettingsService settingsService,
        ILoggingService loggingService,
        BoundaryScanTabViewModel boundaryScanTab)
    {
        _settingsService = settingsService;
        _loggingService = loggingService;
        _boundaryScanTab = boundaryScanTab;

        IsExpanded = settingsService.Settings.IsBoundaryScanEditorExpanded;
    }

    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsBoundaryScanEditorExpanded = value;
    }

    public void LoadTestStep(TestStep testStep)
    {
        if (_observedActions != null)
            _observedActions.CollectionChanged -= PinActionsChanged;

        TestStep = testStep;
        _observedActions = testStep.BoundaryScan.PinActions;
        _observedActions.CollectionChanged += PinActionsChanged;

        // The chain may have changed since the last step was loaded
        if (NewDevice == null || !Devices.Contains(NewDevice))
            NewDevice = Devices.FirstOrDefault();
        else
            RefreshNewPins();

        RebuildActions();
    }

    partial void OnNewDeviceChanged(BoundaryScanDeviceViewModel? value) => RefreshNewPins();

    private void RefreshNewPins()
    {
        var port = NewPin?.Port;

        NewPins.Clear();

        // TAP and power pins cannot be used in a test step
        foreach (var pin in NewDevice?.Pins.Where(p => !p.IsTapPin && (p.CanDrive || p.CanRead)) ?? [])
            NewPins.Add(pin);

        NewPin = NewPins.FirstOrDefault(p => p.Port == port) ?? NewPins.FirstOrDefault();
    }

    private bool CanAddAction() => TestStep != null && NewDevice != null && NewPin != null;

    [RelayCommand(CanExecute = nameof(CanAddAction))]
    private void AddAction()
    {
        if (NewDevice == null || NewPin == null)
            return;

        SetAction(NewDevice, NewPin.Port, NewAction);
    }

    private bool HasSelectedAction() => SelectedAction != null;

    [RelayCommand(CanExecute = nameof(HasSelectedAction))]
    private void RemoveAction()
    {
        if (SelectedAction != null)
            TestStep?.BoundaryScan.PinActions.Remove(SelectedAction.Model);
    }

    /// <summary>
    /// Takes the pin setup of the Boundary Scan tab over into the step: every output changed from its safe value
    /// becomes a drive action, every labeled pin with a captured level becomes an expect action.
    /// </summary>
    [RelayCommand]
    private void TeachIn()
    {
        if (TestStep == null)
            return;

        var count = 0;

        foreach (var device in Devices)
        {
            foreach (var pin in device.GetChangedOutputPins())
            {
                var action = pin.DriveState switch
                {
                    PinDriveState.HIGH => BoundaryScanPinActionType.DRIVE_HIGH,
                    PinDriveState.LOW => BoundaryScanPinActionType.DRIVE_LOW,
                    _ => BoundaryScanPinActionType.DRIVE_Z
                };

                SetAction(device, pin.Port, action);
                count++;
            }

            foreach (var pin in device.Pins.Where(p => p.Label.Trim().Length > 0 && p.InputValue != null))
            {
                SetAction(device, pin.Port, pin.InputValue == true
                    ? BoundaryScanPinActionType.EXPECT_HIGH
                    : BoundaryScanPinActionType.EXPECT_LOW);
                count++;
            }
        }

        if (count == 0)
            _loggingService.Warning("Nothing to take over. Drive outputs or read labeled pins in the Boundary Scan tab first.");
    }

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;

    /// <summary>A pin is driven or checked at most once per step, an existing action of the same kind is replaced.</summary>
    private void SetAction(BoundaryScanDeviceViewModel device, string port, BoundaryScanPinActionType type)
    {
        var actions = TestStep!.BoundaryScan.PinActions;
        var isDrive = BoundaryScanPinAction.IsDriveAction(type);

        var existing = actions.FirstOrDefault(a =>
            a.DeviceId == device.Config.Id &&
            string.Equals(a.Port, port, StringComparison.OrdinalIgnoreCase) &&
            a.IsDrive == isDrive);

        if (existing != null)
        {
            existing.Action = type;
            RebuildActions();
            return;
        }

        actions.Add(new BoundaryScanPinAction { DeviceId = device.Config.Id, Port = port, Action = type });
    }

    private void PinActionsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildActions();

    private void RebuildActions()
    {
        var selected = SelectedAction?.Model;

        Actions.Clear();

        foreach (var action in TestStep?.BoundaryScan.PinActions ?? [])
        {
            var device = Devices.FirstOrDefault(d => d.Config.Id == action.DeviceId);
            Actions.Add(new BoundaryScanPinActionViewModel(action, device));
        }

        SelectedAction = Actions.FirstOrDefault(a => a.Model == selected);
    }
}