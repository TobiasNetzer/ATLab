using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLab.BoundaryScan;
using ATLab.BoundaryScan.Bsdl;
using ATLab.BoundaryScan.Buses;
using ATLab.BoundaryScan.Probes;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class BoundaryScanTestSession : IBoundaryScanTestSession
{
    private readonly IBoundaryScanService _boundaryScanService;
    private readonly ILoggingService _loggingService;
    private readonly ProjectModel _projectModel;

    // Steps and the end of the session must not interleave, a cancel waits for the running scan
    private readonly SemaphoreSlim _lock = new(1, 1);

    private List<SessionDevice>? _devices;

    private sealed class SessionDevice(BoundaryScanDeviceConfig config, BsdlDevice? bsdl, int position)
    {
        public BoundaryScanDeviceConfig Config { get; } = config;
        public string Name { get; } = DeviceName(config, bsdl, position);
        public BsdlDevice? Bsdl { get; } = bsdl;
        public bool[] Outputs { get; } = bsdl?.CreateSafeOutputs() ?? [];
        public BoundaryScanInstruction? ActiveInstruction { get; set; }

        // Once outputs were driven the device stays in EXTEST, leaving it would let the pins glitch between steps
        public bool IsExtest { get; set; }
    }

    public bool IsActive => _devices != null;

    public event Action? ActiveChanged;

    public BoundaryScanTestSession(
        IBoundaryScanService boundaryScanService,
        ILoggingService loggingService,
        ProjectModel projectModel)
    {
        _boundaryScanService = boundaryScanService;
        _loggingService = loggingService;
        _projectModel = projectModel;

        // Loading or creating a project replaces the chain, the session belongs to the old one
        _projectModel.BoundaryScan.Devices.CollectionChanged += (_, _) =>
        {
            if (IsActive)
                _ = EndAsync();
        };
    }

    public async Task<OperationResult<BoundaryScanStepOutcome>> ExecuteStepAsync(BoundaryScanStepConfig step, CancellationToken token)
    {
        if (step.Mode == BoundaryScanStepMode.TURN_OFF)
        {
            await EndAsync();
            return OperationResult<BoundaryScanStepOutcome>.Success(new BoundaryScanStepOutcome(false, 0, []));
        }

        await _lock.WaitAsync(token);
        try
        {
            if (_devices == null)
            {
                var started = await StartAsync();
                if (started.IsFailure)
                    return OperationResult<BoundaryScanStepOutcome>.Failure(started.ErrorMessage);
            }

            return await ExecutePinActionsAsync(_devices!, step, token);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<OperationResult<BusTransactionResult>> ExecuteBusTransactionAsync(
        BoundaryScanBusStepConfig step, byte[] write, CancellationToken token)
    {
        await _lock.WaitAsync(token);
        try
        {
            var bus = _projectModel.BoundaryScan.Buses.FirstOrDefault(b => b.Id == step.BusId);
            if (bus == null)
            {
                return OperationResult<BusTransactionResult>.Failure(string.IsNullOrEmpty(step.BusId)
                    ? "No boundary scan bus selected."
                    : "The selected boundary scan bus no longer exists.");
            }

            if (_devices == null)
            {
                var started = await StartAsync();
                if (started.IsFailure)
                    return OperationResult<BusTransactionResult>.Failure(started.ErrorMessage);
            }

            return await ExecuteBusAsync(_devices!, bus, step, write);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task EndAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_devices == null)
                return;

            // Disconnecting resets all TAP controllers, the chips return to normal operation
            await _boundaryScanService.DisconnectAsync();
            _devices = null;
        }
        finally
        {
            _lock.Release();
        }

        ActiveChanged?.Invoke();
    }

    private async Task<OperationResult> StartAsync()
    {
        var configuration = _projectModel.BoundaryScan;

        if (configuration.Devices.Count == 0)
            return OperationResult.Failure("No boundary scan chain is configured in the Boundary Scan tab.");

        var devices = new List<SessionDevice>();

        for (var i = 0; i < configuration.Devices.Count; i++)
        {
            var config = configuration.Devices[i];
            var bsdl = LoadBsdl(config, i + 1);
            if (bsdl.IsFailure)
                return OperationResult.Failure(bsdl.ErrorMessage);

            devices.Add(new SessionDevice(config, bsdl.Value, i + 1));
        }

        // A connection opened in the Boundary Scan tab is taken over by the test
        if (_boundaryScanService.IsConnected)
            await _boundaryScanService.DisconnectAsync();

        var connected = await _boundaryScanService.ConnectAsync(
            configuration.ProbeType,
            configuration.ProbeSerialNumber,
            new JtagProbeOptions(configuration.FrequencyKHz, configuration.FtdiLowByteValue, configuration.FtdiLowByteDirection));

        if (connected.IsFailure)
            return OperationResult.Failure($"Boundary scan probe: {connected.ErrorMessage}");

        var chain = await _boundaryScanService.DetectChainAsync();
        var verified = chain.IsSuccess
            ? VerifyChain(devices, chain.Value!)
            : OperationResult.Failure($"Boundary scan chain: {chain.ErrorMessage}");

        if (verified.IsFailure)
        {
            await _boundaryScanService.DisconnectAsync();
            return verified;
        }

        _devices = devices;
        ActiveChanged?.Invoke();
        return OperationResult.Success();
    }

    private static OperationResult<BsdlDevice?> LoadBsdl(BoundaryScanDeviceConfig config, int position)
    {
        if (string.IsNullOrWhiteSpace(config.BsdlFilePath))
            return OperationResult<BsdlDevice?>.Success(null);

        if (!File.Exists(config.BsdlFilePath))
            return OperationResult<BsdlDevice?>.Failure($"BSDL file of {DeviceName(config, null, position)} not found: {config.BsdlFilePath}");

        var parsed = BsdlParser.ParseFile(config.BsdlFilePath);
        return parsed.IsSuccess
            ? OperationResult<BsdlDevice?>.Success(parsed.Value)
            : OperationResult<BsdlDevice?>.Failure($"{DeviceName(config, null, position)}: {parsed.ErrorMessage}");
    }

    /// <summary>Makes sure the board in the fixture has the chain the project was set up for.</summary>
    private static OperationResult VerifyChain(List<SessionDevice> devices, JtagChainInfo chain)
    {
        if (chain.Taps.Count != devices.Count)
            return OperationResult.Failure($"Boundary scan chain has {chain.Taps.Count} device(s), the project expects {devices.Count}.");

        for (var i = 0; i < devices.Count; i++)
        {
            var device = devices[i];
            var found = chain.Taps[i].IdCode;
            var foundText = found?.ToString("X8") ?? "none";

            // The BSDL pattern masks the bits marked X, usually the version, so those silicon revisions are accepted too
            var matches = device.Bsdl?.IdCodePattern != null
                ? found is { } idCode && device.Bsdl.MatchesIdCode(idCode)
                : string.IsNullOrEmpty(device.Config.IdCode) || device.Config.IdCode == (found?.ToString("X8") ?? string.Empty);

            if (!matches)
                return OperationResult.Failure($"Boundary scan device {i + 1} ({device.Name}): IDCODE {foundText} does not match the project.");

            if (device.Config.IrLength <= 0)
                return OperationResult.Failure($"Boundary scan device {i + 1} ({device.Name}): the instruction length is unknown.");
        }

        var irLength = devices.Sum(d => d.Config.IrLength);
        if (irLength != chain.TotalIrLength)
            return OperationResult.Failure($"Boundary scan chain: instruction length is {chain.TotalIrLength} bits, the project expects {irLength}.");

        return OperationResult.Success();
    }

    private async Task<OperationResult<BoundaryScanStepOutcome>> ExecutePinActionsAsync(
        List<SessionDevice> devices, BoundaryScanStepConfig step, CancellationToken token)
    {
        var drives = new List<(SessionDevice Device, BsdlCell Cell, BoundaryScanPinAction Action)>();
        var expects = new List<(SessionDevice Device, BsdlCell Cell, BoundaryScanPinAction Action)>();

        foreach (var action in step.PinActions)
        {
            var device = devices.FirstOrDefault(d => d.Config.Id == action.DeviceId);
            if (device == null)
                return OperationResult<BoundaryScanStepOutcome>.Failure($"Boundary scan device of pin {action.Port} is no longer part of the chain.");

            if (device.Bsdl == null)
                return OperationResult<BoundaryScanStepOutcome>.Failure($"{device.Name} has no BSDL file.");

            var cells = device.Bsdl.GetPortCells(action.Port);

            if (action.IsDrive)
            {
                if (cells.Output == null || device.Bsdl.TapPorts.Contains(action.Port))
                    return OperationResult<BoundaryScanStepOutcome>.Failure($"{PinName(device, action.Port)} cannot be driven.");

                drives.Add((device, cells.Output, action));
            }
            else
            {
                if (cells.Input == null)
                    return OperationResult<BoundaryScanStepOutcome>.Failure($"{PinName(device, action.Port)} cannot be read.");

                expects.Add((device, cells.Input, action));
            }
        }

        foreach (var (device, cell, action) in drives)
        {
            var state = action.Action switch
            {
                BoundaryScanPinActionType.DRIVE_HIGH => PinDriveState.HIGH,
                BoundaryScanPinActionType.DRIVE_LOW => PinDriveState.LOW,
                _ => PinDriveState.HIGH_Z
            };

            if (!BsdlDevice.ApplyDrive(device.Outputs, cell, state))
                return OperationResult<BoundaryScanStepOutcome>.Failure($"{PinName(device, action.Port)} cannot be switched to Z.");

            device.IsExtest = true;
        }

        var loaded = await LoadInstructionsAsync(devices, expects.Select(e => e.Device).ToHashSet());
        if (loaded.IsFailure)
            return OperationResult<BoundaryScanStepOutcome>.Failure(loaded.ErrorMessage);

        var entries = Entries(devices);

        // Loading instructions also shifts the outputs, otherwise a data scan updates them
        if (!loaded.Value && drives.Count > 0)
        {
            var updated = await _boundaryScanService.ScanAsync(entries);
            if (updated.IsFailure)
                return OperationResult<BoundaryScanStepOutcome>.Failure($"Boundary scan: {updated.ErrorMessage}");
        }

        if (expects.Count == 0)
            return OperationResult<BoundaryScanStepOutcome>.Success(new BoundaryScanStepOutcome(false, 0, []));

        if (step.SettleTimeMs > 0)
            await Task.Delay(step.SettleTimeMs, token);

        // Pins are captured at the start of a scan, so this one sees the outputs driven above
        var captured = await _boundaryScanService.ScanAsync(entries);
        if (captured.IsFailure)
            return OperationResult<BoundaryScanStepOutcome>.Failure($"Boundary scan: {captured.ErrorMessage}");

        var messages = new List<string>();

        foreach (var (device, cell, action) in expects)
        {
            var expected = action.Action == BoundaryScanPinActionType.EXPECT_HIGH;
            var actual = captured.Value![devices.IndexOf(device)]![cell.Number];

            if (actual != expected)
                messages.Add($"{PinName(device, action.Port)}: expected {(expected ? 1 : 0)}, read {(actual ? 1 : 0)}");
        }

        foreach (var message in messages)
            _loggingService.Warning($"Boundary scan: {message}");

        return OperationResult<BoundaryScanStepOutcome>.Success(new BoundaryScanStepOutcome(true, messages.Count, messages));
    }

    private async Task<OperationResult<BusTransactionResult>> ExecuteBusAsync(
        List<SessionDevice> devices, BoundaryScanBus bus, BoundaryScanBusStepConfig step, byte[] write)
    {
        var readCount = Math.Max(0, step.BytesToRead);

        BusPinDevice? FindDevice(string id) =>
            devices.FirstOrDefault(d => d.Config.Id == id) is { } d ? new BusPinDevice(d.Name, d.Bsdl) : null;

        var problems = BoundaryScanBusValidator.Validate(bus, FindDevice, write: write.Length > 0, read: readCount > 0);
        if (problems.Count > 0)
            return OperationResult<BusTransactionResult>.Failure($"Bus {bus.Name}: {problems[0]}");

        var lines = bus.Pins
            .Where(p => p.IsAssigned)
            .ToDictionary(
                p => new BusLine(p.Signal, p.Index),
                p =>
                {
                    var device = devices.First(d => d.Config.Id == p.DeviceId);
                    return (Device: device, Cells: device.Bsdl!.GetPortCells(p.Port));
                });

        IBusTransaction transaction = bus.Type switch
        {
            BoundaryScanBusType.I2C => new I2CTransaction(step.I2CAddress, write, readCount),
            BoundaryScanBusType.SPI => new SpiTransaction(bus.SpiMode, bus.IsCsActiveHigh, bus.IsLsbFirst, write, readCount,
                lines.Keys.Select(l => l.Signal).ToHashSet()),
            _ => new PinGroupTransaction(bus.Width, write, readCount)
        };

        // Bus pins are driven, so their devices switch to EXTEST and stay there like for pin actions
        foreach (var line in lines.Values)
            line.Device.IsExtest = true;

        var loaded = await LoadInstructionsAsync(devices, new HashSet<SessionDevice>());
        if (loaded.IsFailure)
            return OperationResult<BusTransactionResult>.Failure(loaded.ErrorMessage);

        // Every state is one scan. Scan n captures the pins of state n-1, so one more scan reads the last state.
        var scans = new List<IReadOnlyList<BoundaryScanChainEntry>>();

        foreach (var state in transaction.Waveform.States)
        {
            foreach (var (line, drive) in state)
            {
                // Releasing an input-only pin needs nothing, it is never driven
                if (lines.TryGetValue(line, out var pin) && pin.Cells.Output is { } output)
                    BsdlDevice.ApplyDrive(pin.Device.Outputs, output, drive);
            }

            scans.Add(Entries(devices));
        }

        scans.Add(Entries(devices));

        var captured = await _boundaryScanService.ScanSequenceAsync(scans);
        if (captured.IsFailure)
            return OperationResult<BusTransactionResult>.Failure($"Boundary scan: {captured.ErrorMessage}");

        var samples = transaction.Waveform.Samples
            .Select(s =>
            {
                var pin = lines[s.Line];
                return captured.Value![s.State + 1][devices.IndexOf(pin.Device)]![pin.Cells.Input!.Number];
            })
            .ToList();

        var result = transaction.Decode(samples);

        foreach (var message in result.Messages)
            _loggingService.Warning($"Boundary scan bus {bus.Name}: {message}");

        return OperationResult<BusTransactionResult>.Success(result);
    }

    /// <summary>
    /// Puts driven devices in EXTEST and read devices at least in SAMPLE. Returns true if instructions were loaded,
    /// which also shifted the current outputs.
    /// </summary>
    private async Task<OperationResult<bool>> LoadInstructionsAsync(List<SessionDevice> devices, IReadOnlySet<SessionDevice> readDevices)
    {
        var instructions = devices
            .Select(d => d.IsExtest ? BoundaryScanInstruction.EXTEST
                : readDevices.Contains(d) || d.ActiveInstruction == BoundaryScanInstruction.SAMPLE ? BoundaryScanInstruction.SAMPLE
                : BoundaryScanInstruction.BYPASS)
            .ToList();

        if (!devices.Where((d, i) => d.ActiveInstruction != instructions[i]).Any())
            return OperationResult<bool>.Success(false);

        var entries = devices
            .Select((d, i) => new BoundaryScanChainEntry(d.Config.IrLength, d.Bsdl, instructions[i], d.ActiveInstruction, (bool[])d.Outputs.Clone()))
            .ToList();

        var loaded = await _boundaryScanService.LoadInstructionsAsync(entries);
        if (loaded.IsFailure)
            return OperationResult<bool>.Failure($"Boundary scan: {loaded.ErrorMessage}");

        for (var i = 0; i < devices.Count; i++)
            devices[i].ActiveInstruction = instructions[i];

        return OperationResult<bool>.Success(true);
    }

    private static List<BoundaryScanChainEntry> Entries(List<SessionDevice> devices) =>
        devices
            .Select(d => new BoundaryScanChainEntry(d.Config.IrLength, d.Bsdl, d.ActiveInstruction ?? BoundaryScanInstruction.BYPASS,
                d.ActiveInstruction, (bool[])d.Outputs.Clone()))
            .ToList();

    // Same fallback as the Boundary Scan tab: user name, BSDL entity, position
    private static string DeviceName(BoundaryScanDeviceConfig config, BsdlDevice? bsdl, int position) =>
        !string.IsNullOrWhiteSpace(config.Name) ? config.Name
        : bsdl != null ? bsdl.EntityName
        : $"Device {position}";

    private static string PinName(SessionDevice device, string port)
    {
        var label = device.Config.GetPinLabel(port);
        return label.Length > 0
            ? $"{device.Name} {port} ({label})"
            : $"{device.Name} {port}";
    }
}