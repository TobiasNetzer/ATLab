using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATLab.BoundaryScan;
using ATLab.BoundaryScan.Bsdl;
using ATLab.BoundaryScan.Probes;
using ATLab.Enums;
using ATLab.Models;

namespace ATLab.Interfaces;

/// <param name="IrLength">Length of the instruction register.</param>
/// <param name="Bsdl">Required for every instruction except BYPASS.</param>
/// <param name="Instruction">Instruction the device should be in.</param>
/// <param name="ActiveInstruction">Instruction currently loaded, used to PRELOAD safe values before EXTEST.</param>
/// <param name="Outputs">Boundary register content shifted in, indexed by cell number.</param>
public record BoundaryScanChainEntry(
    int IrLength,
    BsdlDevice? Bsdl,
    BoundaryScanInstruction Instruction,
    BoundaryScanInstruction? ActiveInstruction,
    bool[] Outputs);

public interface IBoundaryScanService
{
    bool IsConnected { get; }

    event EventHandler? ConnectionChanged;

    Task<OperationResult<IReadOnlyList<JtagProbeInfo>>> ListProbesAsync(JtagProbeType probeType);

    Task<OperationResult> ConnectAsync(JtagProbeType probeType, string? serialNumber, JtagProbeOptions options);

    Task DisconnectAsync();

    Task<OperationResult<JtagChainInfo>> DetectChainAsync();

    /// <summary>
    /// Loads the instructions of all devices and captures the boundary registers.
    /// Returns the captured boundary register per device (null for devices in BYPASS), in chain order.
    /// </summary>
    Task<OperationResult<IReadOnlyList<bool[]?>>> LoadInstructionsAsync(IReadOnlyList<BoundaryScanChainEntry> chain);

    /// <summary>
    /// Shifts the outputs into the boundary registers of the loaded instructions and returns the captured values.
    /// </summary>
    Task<OperationResult<IReadOnlyList<bool[]?>>> ScanAsync(IReadOnlyList<BoundaryScanChainEntry> chain);

    /// <summary>Resets all TAP controllers, which returns the devices to normal operation.</summary>
    Task<OperationResult> ResetChainAsync();
}