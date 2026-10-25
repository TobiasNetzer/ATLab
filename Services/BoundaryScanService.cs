using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLab.BoundaryScan;
using ATLab.BoundaryScan.Probes;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class BoundaryScanService : IBoundaryScanService, IDisposable
{
    private const string NotConnectedMessage = "No JTAG probe connected.";

    private readonly SemaphoreSlim _lock = new(1, 1);

    private IJtagProbe? _probe;
    private JtagTap? _tap;

    public bool IsConnected => _probe?.IsOpen == true;

    public event EventHandler? ConnectionChanged;

    public Task<OperationResult<IReadOnlyList<JtagProbeInfo>>> ListProbesAsync(JtagProbeType probeType)
    {
        return Task.Run(() =>
        {
            using var probe = CreateProbe(probeType);
            return probe.ListProbes();
        });
    }

    public async Task<OperationResult> ConnectAsync(JtagProbeType probeType, string? serialNumber, JtagProbeOptions options)
    {
        await DisconnectAsync();

        await _lock.WaitAsync();
        try
        {
            var result = await Task.Run(() =>
            {
                var probe = CreateProbe(probeType);
                var opened = probe.Open(serialNumber, options);

                if (opened.IsFailure)
                {
                    probe.Dispose();
                    return opened;
                }

                _probe = probe;
                _tap = new JtagTap(probe);
                return _tap.Reset();
            });

            if (result.IsFailure)
                CloseProbe();

            return result;
        }
        finally
        {
            _lock.Release();
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task DisconnectAsync()
    {
        if (_probe == null)
            return;

        await _lock.WaitAsync();
        try
        {
            // Leave the devices in Test-Logic-Reset so they return to normal operation
            await Task.Run(() =>
            {
                _tap?.Reset();
                CloseProbe();
            });
        }
        finally
        {
            _lock.Release();
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public Task<OperationResult<JtagChainInfo>> DetectChainAsync() =>
        RunLocked(tap => JtagChainDetector.Detect(tap));

    public Task<OperationResult> ResetChainAsync() =>
        RunLocked(tap => tap.Reset());

    public Task<OperationResult<IReadOnlyList<bool[]?>>> LoadInstructionsAsync(IReadOnlyList<BoundaryScanChainEntry> chain) =>
        RunLocked(tap =>
        {
            var validation = Validate(chain);
            if (validation.IsFailure)
                return OperationResult<IReadOnlyList<bool[]?>>.Failure(validation.ErrorMessage);

            // Output cells drive the pins as soon as EXTEST is active, so the update latches are loaded
            // with the requested values through PRELOAD first
            var needsPreload = chain.Any(e =>
                e.Instruction == BoundaryScanInstruction.EXTEST && e.ActiveInstruction != BoundaryScanInstruction.EXTEST);

            if (needsPreload)
            {
                var preload = chain
                    .Select(e => e.Instruction == BoundaryScanInstruction.EXTEST && e.ActiveInstruction != BoundaryScanInstruction.EXTEST
                        ? e with { Instruction = BoundaryScanInstruction.SAMPLE }
                        : e)
                    .ToList();

                var preloaded = ShiftInstructions(tap, preload, usePreloadOpcode: true);
                if (preloaded.IsFailure)
                    return OperationResult<IReadOnlyList<bool[]?>>.Failure(preloaded.ErrorMessage);

                var shifted = ShiftData(tap, preload);
                if (shifted.IsFailure)
                    return shifted;
            }

            var loaded = ShiftInstructions(tap, chain, usePreloadOpcode: false);
            if (loaded.IsFailure)
                return OperationResult<IReadOnlyList<bool[]?>>.Failure(loaded.ErrorMessage);

            return ShiftData(tap, chain);
        });

    public Task<OperationResult<IReadOnlyList<bool[]?>>> ScanAsync(IReadOnlyList<BoundaryScanChainEntry> chain) =>
        RunLocked(tap =>
        {
            var validation = Validate(chain);
            return validation.IsFailure
                ? OperationResult<IReadOnlyList<bool[]?>>.Failure(validation.ErrorMessage)
                : ShiftData(tap, chain);
        });

    public void Dispose()
    {
        // Devices left in EXTEST would keep driving their pins after the application exits
        _tap?.Reset();
        CloseProbe();
        _lock.Dispose();
    }

    private static OperationResult Validate(IReadOnlyList<BoundaryScanChainEntry> chain)
    {
        if (chain.Count == 0)
            return OperationResult.Failure("The scan chain is empty.");

        for (var i = 0; i < chain.Count; i++)
        {
            var entry = chain[i];

            if (entry.IrLength <= 0)
                return OperationResult.Failure($"Device {i + 1}: the instruction register length is unknown. Assign a BSDL file or enter it manually.");

            if (entry.Instruction == BoundaryScanInstruction.BYPASS)
                continue;

            if (entry.Bsdl == null)
                return OperationResult.Failure($"Device {i + 1}: {entry.Instruction} requires a BSDL file.");

            if (entry.Bsdl.GetOpcode(entry.Instruction.ToString()) == null)
                return OperationResult.Failure($"Device {i + 1}: the BSDL file does not define {entry.Instruction}.");

            if (entry.Outputs.Length != entry.Bsdl.BoundaryLength)
                return OperationResult.Failure($"Device {i + 1}: the boundary register data does not match the BSDL file.");
        }

        return OperationResult.Success();
    }

    private static OperationResult ShiftInstructions(JtagTap tap, IReadOnlyList<BoundaryScanChainEntry> chain, bool usePreloadOpcode)
    {
        var bits = new List<bool>();

        // The first bits shifted end up in the device closest to TDO
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var entry = chain[i];
            bool[]? opcode;

            if (entry.Instruction == BoundaryScanInstruction.BYPASS)
            {
                opcode = Enumerable.Repeat(true, entry.IrLength).ToArray();
            }
            else
            {
                var name = entry.Instruction.ToString();

                // 1149.1-2001 splits SAMPLE/PRELOAD, older files only know SAMPLE which also preloads
                if (usePreloadOpcode && entry.Instruction == BoundaryScanInstruction.SAMPLE && entry.Bsdl!.HasInstruction("PRELOAD"))
                    name = "PRELOAD";

                opcode = entry.Bsdl!.GetOpcode(name);

                if (opcode == null)
                    return OperationResult.Failure($"Device {i + 1}: the BSDL file does not define {name}.");
            }

            if (opcode.Length != entry.IrLength)
                return OperationResult.Failure($"Device {i + 1}: the opcode length does not match the instruction register length {entry.IrLength}.");

            bits.AddRange(opcode);
        }

        var result = tap.ScanIr(bits.ToArray());
        return result.IsSuccess ? OperationResult.Success() : OperationResult.Failure(result.ErrorMessage);
    }

    private static OperationResult<IReadOnlyList<bool[]?>> ShiftData(JtagTap tap, IReadOnlyList<BoundaryScanChainEntry> chain)
    {
        var bits = new List<bool>();

        for (var i = chain.Count - 1; i >= 0; i--)
        {
            if (chain[i].Instruction == BoundaryScanInstruction.BYPASS)
                bits.Add(false);
            else
                bits.AddRange(chain[i].Outputs);
        }

        var result = tap.ScanDr(bits.ToArray());
        if (result.IsFailure)
            return OperationResult<IReadOnlyList<bool[]?>>.Failure(result.ErrorMessage);

        var tdo = result.Value!;
        var captured = new bool[]?[chain.Count];
        var position = 0;

        for (var i = chain.Count - 1; i >= 0; i--)
        {
            if (chain[i].Instruction == BoundaryScanInstruction.BYPASS)
            {
                position++;
                continue;
            }

            var length = chain[i].Outputs.Length;
            captured[i] = tdo.Skip(position).Take(length).ToArray();
            position += length;
        }

        return OperationResult<IReadOnlyList<bool[]?>>.Success(captured);
    }

    private Task<OperationResult> RunLocked(Func<JtagTap, OperationResult> action) =>
        RunLocked(action, () => OperationResult.Failure(NotConnectedMessage));

    private Task<OperationResult<T>> RunLocked<T>(Func<JtagTap, OperationResult<T>> action) =>
        RunLocked(action, () => OperationResult<T>.Failure(NotConnectedMessage));

    private async Task<TResult> RunLocked<TResult>(Func<JtagTap, TResult> action, Func<TResult> notConnected)
    {
        await _lock.WaitAsync();
        try
        {
            return await Task.Run(() => _tap != null && IsConnected ? action(_tap) : notConnected());
        }
        finally
        {
            _lock.Release();
        }
    }

    private void CloseProbe()
    {
        _probe?.Dispose();
        _probe = null;
        _tap = null;
    }

    private static IJtagProbe CreateProbe(JtagProbeType probeType) => probeType switch
    {
        JtagProbeType.JLINK => new JLinkProbe(),
        JtagProbeType.FTDI_MPSSE => new FtdiMpsseProbe(),
        _ => throw new ArgumentOutOfRangeException(nameof(probeType))
    };
}