using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ATLab.BoundaryScan;
using ATLab.Services;
using ATLab.BoundaryScan.Bsdl;
using ATLab.BoundaryScan.Probes;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

/// <summary>
/// Boundary Scan tab: probe connection, chain detection and comparison, device list with BSDL files, instruction
/// loading, live pin view and the bus definitions. Locked while a test controls the chain.
/// </summary>
public partial class BoundaryScanTabViewModel : ViewModelBase
{
    private const int RefreshIntervalMs = 100;

    private readonly IBoundaryScanService _boundaryScanService;
    private readonly IBoundaryScanTestSession _testSession;
    private readonly ILoggingService _loggingService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly ProjectModel _projectModel;

    private JtagChainInfo? _detectedChain;
    private bool _isAutoRefreshRunning;
    private bool _isMergingChain;
    private bool _isUpdatingConfiguration;

    public BoundaryScanConfiguration Configuration => _projectModel.BoundaryScan;

    public List<JtagProbeType> AvailableProbeTypes { get; } = new(Enum.GetValues<JtagProbeType>());

    public ObservableCollection<JtagProbeInfo> AvailableProbes { get; } = new();

    public ObservableCollection<BoundaryScanDeviceViewModel> Devices { get; } = new();

    public BoundaryScanBusEditorViewModel BusEditor { get; }

    /// <summary>The right panel shows either the pins of the selected device or the bus setup.</summary>
    [ObservableProperty]
    private bool _isBusView;

    [ObservableProperty]
    private JtagProbeInfo? _selectedProbe;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedDevice))]
    [NotifyCanExecuteChangedFor(nameof(RemoveDeviceCommand), nameof(ImportBsdlCommand), nameof(ClearBsdlCommand),
        nameof(LoadInstructionCommand), nameof(ResetOutputsCommand))]
    private BoundaryScanDeviceViewModel? _selectedDevice;

    [ObservableProperty]
    private BoundaryScanPinViewModel? _selectedPin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotConnected), nameof(CanEditChain))]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand), nameof(DisconnectCommand), nameof(ScanChainCommand),
        nameof(LoadInstructionCommand), nameof(RefreshCommand), nameof(AddDeviceCommand), nameof(RemoveDeviceCommand),
        nameof(AdoptChainCommand))]
    private bool _isConnected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditChain))]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand), nameof(DisconnectCommand), nameof(ScanChainCommand),
        nameof(LoadInstructionCommand), nameof(RefreshCommand), nameof(RefreshProbesCommand), nameof(AdoptChainCommand))]
    private bool _isBusy;

    /// <summary>A running test owns the probe, the tab only shows the configuration meanwhile.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private bool _isControlledByTest;

    [ObservableProperty]
    private string _chainStatus = string.Empty;

    [ObservableProperty]
    private bool? _isChainValid;

    [ObservableProperty]
    private bool _isAutoRefresh;

    [ObservableProperty]
    private string _pinFilter = string.Empty;

    [ObservableProperty]
    private bool _showLinkagePins = true;

    public bool IsNotConnected => !IsConnected;
    public bool HasSelectedDevice => SelectedDevice != null;

    // The chain is only changed while no instruction is loaded, otherwise hardware and configuration diverge
    public bool CanEditChain => !IsConnected && !IsBusy;

    public bool IsFtdiProbe => Configuration.ProbeType == JtagProbeType.FTDI_MPSSE;

    public string FtdiLowByteValueText
    {
        get => Configuration.FtdiLowByteValue.ToString("X2");
        set
        {
            if (byte.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed))
                Configuration.FtdiLowByteValue = parsed;
        }
    }

    public string FtdiLowByteDirectionText
    {
        get => Configuration.FtdiLowByteDirection.ToString("X2");
        set
        {
            if (byte.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed))
                Configuration.FtdiLowByteDirection = parsed;
        }
    }

    public BoundaryScanTabViewModel(
        IBoundaryScanService boundaryScanService,
        ILoggingService loggingService,
        IFileDialogService fileDialogService,
        IMessageBoxService messageBoxService,
        IBoundaryScanTestSession testSession,
        ProjectModel projectModel)
    {
        _boundaryScanService = boundaryScanService;
        _loggingService = loggingService;
        _fileDialogService = fileDialogService;
        _messageBoxService = messageBoxService;
        _projectModel = projectModel;
        _testSession = testSession;

        _boundaryScanService.ConnectionChanged += (_, _) => Dispatcher.UIThread.Post(OnServiceConnectionChanged);
        _testSession.ActiveChanged += () => Dispatcher.UIThread.Post(() => IsControlledByTest = _testSession.IsActive);

        SetLocalizedTitle("Tab_BoundaryScan");

        BusEditor = new BoundaryScanBusEditorViewModel(Configuration, Devices);

        Configuration.Devices.CollectionChanged += ConfiguredDevicesChanged;
        Configuration.ConfigurationChanged += ConfigurationChanged;
        Configuration.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(BoundaryScanConfiguration.ProbeType):
                    OnPropertyChanged(nameof(IsFtdiProbe));
                    AvailableProbes.Clear();
                    break;
                case nameof(BoundaryScanConfiguration.FtdiLowByteValue):
                    OnPropertyChanged(nameof(FtdiLowByteValueText));
                    break;
                case nameof(BoundaryScanConfiguration.FtdiLowByteDirection):
                    OnPropertyChanged(nameof(FtdiLowByteDirectionText));
                    break;
            }
        };

        SyncDevices();
    }

    partial void OnSelectedProbeChanged(JtagProbeInfo? value)
    {
        if (value != null)
            Configuration.ProbeSerialNumber = value.SerialNumber;
    }

    partial void OnSelectedDeviceChanged(BoundaryScanDeviceViewModel? value)
    {
        SelectedPin = null;
        value?.ApplyPinFilter(PinFilter, ShowLinkagePins);
    }

    partial void OnPinFilterChanged(string value) => SelectedDevice?.ApplyPinFilter(value, ShowLinkagePins);

    partial void OnShowLinkagePinsChanged(bool value) => SelectedDevice?.ApplyPinFilter(PinFilter, value);

    partial void OnIsAutoRefreshChanged(bool value)
    {
        if (value)
            _ = AutoRefreshLoop();
    }

    [RelayCommand]
    private void ToggleAutoRefresh() => IsAutoRefresh = !IsAutoRefresh;

    [RelayCommand]
    private void ShowPins() => IsBusView = false;

    [RelayCommand]
    private void ShowBuses() => IsBusView = true;

    private bool CanRefreshProbes() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanRefreshProbes))]
    private async Task RefreshProbes()
    {
        var result = await _boundaryScanService.ListProbesAsync(Configuration.ProbeType);

        AvailableProbes.Clear();

        if (result.IsFailure)
        {
            _loggingService.Error(result.ErrorMessage);
            return;
        }

        foreach (var probe in result.Value!)
            AvailableProbes.Add(probe);

        if (result.Value!.Count == 0)
            _loggingService.Warning("No JTAG probe found.");
    }

    private bool CanConnect() => !IsConnected && !IsBusy && !IsControlledByTest;

    /// <summary>
    /// The connection can also be closed from outside, e.g. when a test run takes over the probe.
    /// </summary>
    private void OnServiceConnectionChanged()
    {
        if (!IsConnected || _boundaryScanService.IsConnected)
            return;

        IsAutoRefresh = false;
        IsConnected = false;
        ForgetDetectedChain();

        foreach (var device in Devices)
            device.SetActiveInstruction(null);

        UpdateChainStatus();
    }

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task Connect()
    {
        IsBusy = true;
        try
        {
            var options = new JtagProbeOptions(
                Configuration.FrequencyKHz,
                Configuration.FtdiLowByteValue,
                Configuration.FtdiLowByteDirection);

            var result = await _boundaryScanService.ConnectAsync(
                Configuration.ProbeType, Configuration.ProbeSerialNumber, options);

            if (result.IsFailure)
            {
                _loggingService.Error(result.ErrorMessage);
                return;
            }

            IsConnected = true;
            _loggingService.Info("JTAG probe connected.");

            foreach (var device in Devices)
                device.SetActiveInstruction(null);

            await DetectChain();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanDisconnect() => IsConnected && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    private async Task Disconnect()
    {
        IsAutoRefresh = false;
        IsBusy = true;
        try
        {
            await _boundaryScanService.DisconnectAsync();
            IsConnected = false;
            ForgetDetectedChain();

            foreach (var device in Devices)
                device.SetActiveInstruction(null);

            _loggingService.Info("JTAG probe disconnected.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanScanChain() => IsConnected && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanScanChain))]
    private async Task ScanChain()
    {
        IsAutoRefresh = false;
        IsBusy = true;
        try
        {
            await DetectChain();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanAddDevice() => !IsConnected;

    [RelayCommand(CanExecute = nameof(CanAddDevice))]
    private void AddDevice()
    {
        var device = new BoundaryScanDeviceConfig();
        Configuration.Devices.Add(device);
        SelectedDevice = Devices.FirstOrDefault(d => d.Config == device);
    }

    private bool CanRemoveDevice() => !IsConnected && SelectedDevice != null;

    [RelayCommand(CanExecute = nameof(CanRemoveDevice))]
    private void RemoveDevice()
    {
        if (SelectedDevice != null)
            Configuration.Devices.Remove(SelectedDevice.Config);
    }

    private bool HasDevice() => SelectedDevice != null;

    [RelayCommand(CanExecute = nameof(HasDevice))]
    private async Task ImportBsdl()
    {
        if (SelectedDevice == null)
            return;

        var file = await _fileDialogService.OpenFileAsync("Import BSDL File", ["bsd", "bsdl"]);
        if (file == null)
            return;

        await AssignBsdl(SelectedDevice, file.Path.LocalPath);
    }

    public async Task AssignBsdl(BoundaryScanDeviceViewModel device, string path)
    {
        await ReleaseDevice(device);

        device.Config.BsdlFilePath = path;
        LoadInstructionCommand.NotifyCanExecuteChanged();
        InferInstructionLength();

        if (device.Bsdl == null)
        {
            _loggingService.Error(device.BsdlError);
            return;
        }

        foreach (var warning in device.Bsdl.Warnings)
            _loggingService.Warning($"{device.Bsdl.EntityName}: {warning}");

        if (device.IdCodeMatches == false)
            _loggingService.Warning($"The IDCODE 0x{device.Config.IdCode} of {device.DisplayName} does not match the BSDL file {device.Bsdl.EntityName}.");
    }

    [RelayCommand(CanExecute = nameof(HasDevice))]
    private async Task ClearBsdl()
    {
        var device = SelectedDevice;
        if (device == null)
            return;

        await ReleaseDevice(device);
        device.Config.BsdlFilePath = string.Empty;
        LoadInstructionCommand.NotifyCanExecuteChanged();
        InferInstructionLength();
    }

    private bool CanLoadInstruction(BoundaryScanInstruction instruction) =>
        IsConnected && !IsBusy && SelectedDevice != null &&
        (instruction == BoundaryScanInstruction.BYPASS || SelectedDevice.HasBsdl);

    [RelayCommand(CanExecute = nameof(CanLoadInstruction))]
    private async Task LoadInstruction(BoundaryScanInstruction instruction)
    {
        if (SelectedDevice == null)
            return;

        IsBusy = true;
        try
        {
            await LoadInstructions(new Dictionary<BoundaryScanDeviceViewModel, BoundaryScanInstruction>
            {
                [SelectedDevice] = instruction
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRefresh() => IsConnected && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task Refresh()
    {
        await Scan();
    }

    [RelayCommand(CanExecute = nameof(HasDevice))]
    private async Task ResetOutputs()
    {
        if (SelectedDevice == null)
            return;

        SelectedDevice.ResetOutputsToSafeValues();
        await ApplyOutputs(SelectedDevice);
    }

    /// <summary>
    /// Scans the chain and compares it with the configuration. An existing configuration is never changed here,
    /// a different board must not wipe the setup. Only an empty configuration takes over the detected chain.
    /// </summary>
    private async Task DetectChain()
    {
        var result = await _boundaryScanService.DetectChainAsync();

        if (result.IsFailure)
        {
            ForgetDetectedChain();
            ChainStatus = result.ErrorMessage;
            IsChainValid = false;
            _loggingService.Error($"Scan chain detection failed: {result.ErrorMessage}");
            return;
        }

        _detectedChain = result.Value!;
        AdoptChainCommand.NotifyCanExecuteChanged();

        if (Configuration.Devices.Count == 0)
            MergeDetectedChain(_detectedChain);

        ApplyDetectedIdCodes();
        InferInstructionLength();
        UpdateChainStatus();
        ActivateBypassIfValid();

        if (IsChainValid == true)
            _loggingService.Info($"Scan chain: {_detectedChain.Taps.Count} device(s) found.");
        else
            _loggingService.Warning($"The scan chain does not match the project: {ChainStatus}");
    }

    private bool CanAdoptChain() => IsConnected && !IsBusy && _detectedChain != null;

    /// <summary>Replaces the configured chain with the detected one, on purpose and after confirmation.</summary>
    [RelayCommand(CanExecute = nameof(CanAdoptChain))]
    private async Task AdoptChain()
    {
        if (_detectedChain == null)
            return;

        if (Configuration.Devices.Count > 0)
        {
            var confirmed = await _messageBoxService.ShowConfirmationDestructiveAsync(
                LocalizationService.Instance["Title_AdoptChain"],
                string.Format(LocalizationService.Instance["Text_AdoptChain"], _detectedChain.Taps.Count));

            if (!confirmed)
                return;
        }

        IsAutoRefresh = false;
        await _boundaryScanService.ResetChainAsync();

        MergeDetectedChain(_detectedChain);
        ApplyDetectedIdCodes();
        InferInstructionLength();
        UpdateChainStatus();
        ActivateBypassIfValid();

        _loggingService.Info($"Scan chain: configuration replaced by the {_detectedChain.Taps.Count} detected device(s).");
    }

    /// <summary>
    /// Builds the configuration from the detected chain. Devices that are found again (same IDCODE, or matching
    /// their BSDL file) keep their settings, all others are replaced.
    /// </summary>
    private void MergeDetectedChain(JtagChainInfo chain)
    {
        var previous = Configuration.Devices.ToList();
        var unused = previous.ToList();
        var merged = new List<BoundaryScanDeviceConfig>();

        BsdlDevice? BsdlOf(BoundaryScanDeviceConfig config) => Devices.FirstOrDefault(d => d.Config == config)?.Bsdl;

        for (var i = 0; i < chain.Taps.Count; i++)
        {
            var found = chain.Taps[i].IdCode;
            var idCode = found?.ToString("X8") ?? string.Empty;

            // Prefer the device at the same position, then any other device of the same type
            var match = i < previous.Count && unused.Contains(previous[i]) &&
                        BoundaryScanChainMatcher.IsSameDevice(previous[i], BsdlOf(previous[i]), found)
                ? previous[i]
                : unused.FirstOrDefault(d => found != null &&
                                             (d.IdCode == idCode || BsdlOf(d)?.IdCodePattern != null && BoundaryScanChainMatcher.IsSameDevice(d, BsdlOf(d), found)));

            match ??= new BoundaryScanDeviceConfig();
            match.IdCode = idCode;

            unused.Remove(match);
            merged.Add(match);
        }

        if (!merged.SequenceEqual(previous))
        {
            _isMergingChain = true;
            try
            {
                Configuration.Devices.Clear();
                foreach (var device in merged)
                    Configuration.Devices.Add(device);
            }
            finally
            {
                _isMergingChain = false;
            }

            SyncDevices();
        }
    }

    private void ApplyDetectedIdCodes()
    {
        for (var i = 0; i < Devices.Count; i++)
            Devices[i].DetectedIdCode = _detectedChain != null && i < _detectedChain.Taps.Count ? _detectedChain.Taps[i].IdCode : null;
    }

    private void ActivateBypassIfValid()
    {
        // After detection all devices are reset, which is equivalent to BYPASS for the data scans that follow
        foreach (var device in Devices)
            device.SetActiveInstruction(IsChainValid == true ? BoundaryScanInstruction.BYPASS : null);
    }

    private void ForgetDetectedChain()
    {
        _detectedChain = null;
        AdoptChainCommand.NotifyCanExecuteChanged();

        foreach (var device in Devices)
            device.DetectedIdCode = null;
    }

    /// <summary>
    /// The instruction register length of a device without BSDL follows from the chain total
    /// as long as it is the only device whose length is unknown and the chain matches the configuration.
    /// </summary>
    private void InferInstructionLength()
    {
        if (_detectedChain == null || _detectedChain.Taps.Count != Devices.Count)
            return;

        var unknown = Devices.Where(d => !d.HasBsdl && d.Config.IrLength <= 0).ToList();
        if (unknown.Count != 1)
            return;

        var known = Devices.Except(unknown).Sum(d => d.Config.IrLength);
        unknown[0].Config.IrLength = Math.Max(0, _detectedChain.TotalIrLength - known);
    }

    private void ConfigurationChanged()
    {
        if (_isUpdatingConfiguration)
            return;

        _isUpdatingConfiguration = true;
        try
        {
            InferInstructionLength();
            UpdateChainStatus();
        }
        finally
        {
            _isUpdatingConfiguration = false;
        }
    }

    private void UpdateChainStatus()
    {
        if (_detectedChain == null)
        {
            ChainStatus = string.Empty;
            IsChainValid = null;
            return;
        }

        var problems = BoundaryScanChainMatcher.Compare(
            Devices.Select(d => new ConfiguredChainDevice(d.DisplayName, d.Config, d.Bsdl)).ToList(),
            _detectedChain);

        if (problems.Count > 0)
        {
            ChainStatus = string.Join(Environment.NewLine, problems);
            IsChainValid = false;
        }
        else
        {
            ChainStatus = $"{_detectedChain.Taps.Count} device(s), instruction register {_detectedChain.TotalIrLength} bits";
            IsChainValid = true;
        }
    }

    private async Task LoadInstructions(IReadOnlyDictionary<BoundaryScanDeviceViewModel, BoundaryScanInstruction> changes)
    {
        UpdateChainStatus();

        if (_detectedChain == null || IsChainValid != true)
        {
            _loggingService.Error($"Instructions cannot be loaded: {(_detectedChain == null ? "scan the chain first." : ChainStatus)}");
            return;
        }

        var instructions = Devices
            .Select(d => changes.TryGetValue(d, out var changed) ? changed : d.ActiveInstruction ?? BoundaryScanInstruction.BYPASS)
            .ToList();

        var entries = Devices.Select((d, i) => d.ToChainEntry(instructions[i])).ToList();
        var result = await _boundaryScanService.LoadInstructionsAsync(entries);

        if (result.IsFailure)
        {
            _loggingService.Error($"Loading instructions failed: {result.ErrorMessage}");
            return;
        }

        for (var i = 0; i < Devices.Count; i++)
        {
            Devices[i].SetActiveInstruction(instructions[i]);
            Devices[i].ApplyCapture(result.Value![i]);
        }
    }

    private async Task Scan()
    {
        if (!IsConnected || Devices.All(d => d.ActiveInstruction is null or BoundaryScanInstruction.BYPASS))
            return;

        var entries = Devices.Select(d => d.ToChainEntry(d.ActiveInstruction ?? BoundaryScanInstruction.BYPASS)).ToList();
        var result = await _boundaryScanService.ScanAsync(entries);

        if (result.IsFailure)
        {
            IsAutoRefresh = false;
            _loggingService.Error($"Boundary scan failed: {result.ErrorMessage}");
            return;
        }

        for (var i = 0; i < Devices.Count && i < result.Value!.Count; i++)
            Devices[i].ApplyCapture(result.Value[i]);
    }

    private async Task ApplyOutputs(BoundaryScanDeviceViewModel device)
    {
        if (!IsConnected || device.ActiveInstruction != BoundaryScanInstruction.EXTEST)
            return;

        // Pins are captured before the new values are updated, the second scan reads back the result
        await Scan();
        await Scan();
    }

    private async Task AutoRefreshLoop()
    {
        if (_isAutoRefreshRunning)
            return;

        _isAutoRefreshRunning = true;
        try
        {
            while (IsAutoRefresh && IsConnected)
            {
                if (!IsBusy)
                    await Scan();

                await Task.Delay(RefreshIntervalMs);
            }
        }
        finally
        {
            _isAutoRefreshRunning = false;
        }
    }

    /// <summary>Switches a device back to BYPASS before its BSDL changes.</summary>
    private async Task ReleaseDevice(BoundaryScanDeviceViewModel device)
    {
        if (!IsConnected || device.ActiveInstruction is null or BoundaryScanInstruction.BYPASS)
            return;

        await LoadInstructions(new Dictionary<BoundaryScanDeviceViewModel, BoundaryScanInstruction>
        {
            [device] = BoundaryScanInstruction.BYPASS
        });
    }

    private void ConfiguredDevicesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // The merge replaces all devices at once and syncs afterwards
        if (_isMergingChain)
            return;

        SyncDevices();

        // A project was loaded or created while connected, the loaded instructions no longer belong to this chain
        if (IsConnected && _detectedChain != null)
        {
            ForgetDetectedChain();
            IsAutoRefresh = false;
            _ = _boundaryScanService.ResetChainAsync();

            foreach (var device in Devices)
                device.SetActiveInstruction(null);

            UpdateChainStatus();
        }
    }

    /// <summary>
    /// Mirrors the configured devices, existing view models are kept so their BSDL and pin states survive.
    /// </summary>
    private void SyncDevices()
    {
        var selected = SelectedDevice?.Config;
        var existing = Devices.ToDictionary(d => d.Config);

        foreach (var device in existing.Values.Where(d => !Configuration.Devices.Contains(d.Config)))
        {
            device.OutputsChanged -= DeviceOutputsChanged;
            device.Detach();
        }

        Devices.Clear();

        for (var i = 0; i < Configuration.Devices.Count; i++)
        {
            var config = Configuration.Devices[i];

            if (existing.TryGetValue(config, out var device))
            {
                device.Position = i + 1;
            }
            else
            {
                device = new BoundaryScanDeviceViewModel(config, i + 1);
                device.OutputsChanged += DeviceOutputsChanged;
            }

            Devices.Add(device);
        }

        SelectedDevice = Devices.FirstOrDefault(d => d.Config == selected) ?? Devices.FirstOrDefault();
        ApplyDetectedIdCodes();
        UpdateChainStatus();
    }

    private async void DeviceOutputsChanged(BoundaryScanDeviceViewModel device)
    {
        try
        {
            await ApplyOutputs(device);
        }
        catch (Exception ex)
        {
            _loggingService.Error(ex.Message);
        }
    }
}