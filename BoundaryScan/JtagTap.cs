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

    public OperationResult<bool[]> ScanIr(bool[] tdi) => Scan(tdi, [true, true, false, false]);

    public OperationResult<bool[]> ScanDr(bool[] tdi) => Scan(tdi, [true, false, false]);

    private OperationResult<bool[]> Scan(bool[] tdi, bool[] enterShift)
    {
        if (tdi.Length == 0)
            return OperationResult<bool[]>.Failure("Scan length must not be zero.");

        // Shift state is entered, the last data bit moves to Exit1, then Update and back to Run-Test/Idle
        var length = enterShift.Length + tdi.Length + 2;
        var tms = new bool[length];
        var input = new bool[length];

        enterShift.CopyTo(tms, 0);
        tdi.CopyTo(input, enterShift.Length);
        tms[enterShift.Length + tdi.Length - 1] = true;
        tms[enterShift.Length + tdi.Length] = true;

        var result = _probe.Shift(tms, input);
        if (result.IsFailure)
            return result;

        return OperationResult<bool[]>.Success(result.Value!.Skip(enterShift.Length).Take(tdi.Length).ToArray());
    }
}