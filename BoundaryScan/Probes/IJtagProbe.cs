using System;
using System.Collections.Generic;
using ATLab.Models;

namespace ATLab.BoundaryScan.Probes;

public record JtagProbeInfo(string SerialNumber, string Description)
{
    public string DisplayName => string.IsNullOrWhiteSpace(SerialNumber)
        ? Description
        : $"{Description} ({SerialNumber})";
}

public record JtagProbeOptions(int FrequencyKHz, byte FtdiLowByteValue = 0x08, byte FtdiLowByteDirection = 0x0B);

/// <summary>
/// Raw JTAG access. All higher level operations (TAP navigation, IR/DR scans) are built on <see cref="Shift"/>.
/// Implementations are not thread safe, callers serialize access.
/// </summary>
public interface IJtagProbe : IDisposable
{
    bool IsOpen { get; }

    OperationResult<IReadOnlyList<JtagProbeInfo>> ListProbes();

    OperationResult Open(string? serialNumber, JtagProbeOptions options);

    void Close();

    /// <summary>
    /// Clocks TCK once per element. TMS and TDI are applied before the rising edge, TDO is sampled on it.
    /// </summary>
    OperationResult<bool[]> Shift(bool[] tms, bool[] tdi);
}