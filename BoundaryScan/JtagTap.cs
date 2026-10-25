using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.BoundaryScan.Probes;
using ATLab.Models;

namespace ATLab.BoundaryScan;

/// <summary>
/// TAP controller navigation on top of a raw probe. Every operation starts and ends in Run-Test/Idle.
/// Bit arrays are LSB first, index 0 is the first bit shifted in on TDI and the first bit read from TDO.
/// </summary>
public class JtagTap
{
    private readonly IJtagProbe _probe;

    public JtagTap(IJtagProbe probe)
    {
        _probe = probe;
    }

    public OperationResult Reset()
    {
        // Five clocks with TMS high reach Test-Logic-Reset from any state
        var result = _probe.Shift([true, true, true, true, true, false], new bool[6]);
        return result.IsSuccess ? OperationResult.Success() : OperationResult.Failure(result.ErrorMessage);
    }

    public OperationResult<bool[]> ScanIr(bool[] tdi) => Single(ScanSequence([tdi], EnterShiftIr));

    public OperationResult<bool[]> ScanDr(bool[] tdi) => Single(ScanSequence([tdi], EnterShiftDr));

    /// <summary>
    /// Runs several data register scans back to back in a single probe transfer. Each scan passes Update-DR,
    /// so it applies its outputs, and captures the pins that the previous scan left behind.
    /// </summary>
    public OperationResult<bool[][]> ScanDrSequence(IReadOnlyList<bool[]> tdis) => ScanSequence(tdis, EnterShiftDr);

    private static OperationResult<bool[]> Single(OperationResult<bool[][]> result) =>
        result.IsSuccess
            ? OperationResult<bool[]>.Success(result.Value![0])
            : OperationResult<bool[]>.Failure(result.ErrorMessage);

    private static readonly bool[] EnterShiftIr = [true, true, false, false];
    private static readonly bool[] EnterShiftDr = [true, false, false];

    private OperationResult<bool[][]> ScanSequence(IReadOnlyList<bool[]> tdis, bool[] enterShift)
    {
        if (tdis.Count == 0 || tdis.Any(t => t.Length == 0))
            return OperationResult<bool[][]>.Failure("Scan length must not be zero.");

        // Each scan enters the shift state, the last data bit moves to Exit1, then Update and back to Run-Test/Idle
        var length = tdis.Sum(t => enterShift.Length + t.Length + 2);
        var tms = new bool[length];
        var input = new bool[length];
        var dataStarts = new int[tdis.Count];
        var position = 0;

        for (var i = 0; i < tdis.Count; i++)
        {
            var tdi = tdis[i];

            enterShift.CopyTo(tms, position);
            dataStarts[i] = position + enterShift.Length;
            tdi.CopyTo(input, dataStarts[i]);
            tms[dataStarts[i] + tdi.Length - 1] = true;
            tms[dataStarts[i] + tdi.Length] = true;

            position = dataStarts[i] + tdi.Length + 2;
        }

        var result = _probe.Shift(tms, input);
        if (result.IsFailure)
            return OperationResult<bool[][]>.Failure(result.ErrorMessage);

        var tdo = result.Value!;
        return OperationResult<bool[][]>.Success(
            tdis.Select((t, i) => tdo.AsSpan(dataStarts[i], t.Length).ToArray()).ToArray());
    }
}