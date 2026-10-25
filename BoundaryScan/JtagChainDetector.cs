using System.Collections.Generic;
using System.Linq;
using ATLab.Models;

namespace ATLab.BoundaryScan;

/// <param name="IdCode">Null for devices that select BYPASS after reset because they have no IDCODE register.</param>
public record DetectedTap(uint? IdCode);

/// <param name="Taps">In chain order, the first device is connected to TDI.</param>
public record JtagChainInfo(IReadOnlyList<DetectedTap> Taps, int TotalIrLength);

/// <summary>
/// Detects the scan chain without any BSDL knowledge using only the mandatory IEEE 1149.1 behavior.
/// </summary>
public static class JtagChainDetector
{
    private const int MaxTotalIrLength = 1024;
    private const int MaxDevices = 128;

    public static OperationResult<JtagChainInfo> Detect(JtagTap tap)
    {
        var reset = tap.Reset();
        if (reset.IsFailure)
            return OperationResult<JtagChainInfo>.Failure(reset.ErrorMessage);

        var irLength = MeasureLength(tap.ScanIr, MaxTotalIrLength);
        if (irLength.IsFailure)
            return OperationResult<JtagChainInfo>.Failure($"Instruction register: {irLength.ErrorMessage}");

        if (irLength.Value == 0)
            return OperationResult<JtagChainInfo>.Failure("No devices found in the scan chain.");

        // The IR scan above shifted ones into every instruction register, so all devices are in BYPASS
        // and every device contributes exactly one bit to the data register
        var deviceCount = MeasureLength(tap.ScanDr, MaxDevices);
        if (deviceCount.IsFailure)
            return OperationResult<JtagChainInfo>.Failure($"Bypass register: {deviceCount.ErrorMessage}");

        if (deviceCount.Value == 0)
            return OperationResult<JtagChainInfo>.Failure("No devices found in the scan chain.");

        var idCodes = ReadIdCodes(tap, deviceCount.Value);
        if (idCodes.IsFailure)
            return OperationResult<JtagChainInfo>.Failure(idCodes.ErrorMessage);

        // Return the devices to their reset state (IDCODE or BYPASS)
        tap.Reset();

        return OperationResult<JtagChainInfo>.Success(new JtagChainInfo(idCodes.Value!, irLength.Value));
    }

    /// <summary>
    /// Flushes the register with zeros, then shifts ones and counts the clocks until the first one appears on TDO.
    /// </summary>
    private static OperationResult<int> MeasureLength(System.Func<bool[], OperationResult<bool[]>> scan, int maxLength)
    {
        var tdi = new bool[maxLength * 2];
        for (var i = maxLength; i < tdi.Length; i++)
            tdi[i] = true;

        var result = scan(tdi);
        if (result.IsFailure)
            return OperationResult<int>.Failure(result.ErrorMessage);

        var tdo = result.Value!;

        if (tdo.All(b => b))
            return OperationResult<int>.Failure("TDO is stuck high. Check the probe connection and the target power.");

        if (tdo.All(b => !b))
            return OperationResult<int>.Failure("TDO is stuck low. Check the probe connection and the target power.");

        for (var i = maxLength; i < tdo.Length; i++)
        {
            if (tdo[i])
                return OperationResult<int>.Success(i - maxLength);
        }

        return OperationResult<int>.Failure($"The chain is longer than {maxLength} bits or broken.");
    }

    private static OperationResult<IReadOnlyList<DetectedTap>> ReadIdCodes(JtagTap tap, int deviceCount)
    {
        var reset = tap.Reset();
        if (reset.IsFailure)
            return OperationResult<IReadOnlyList<DetectedTap>>.Failure(reset.ErrorMessage);

        var tdi = Enumerable.Repeat(true, deviceCount * 32).ToArray();
        var result = tap.ScanDr(tdi);
        if (result.IsFailure)
            return OperationResult<IReadOnlyList<DetectedTap>>.Failure(result.ErrorMessage);

        var tdo = result.Value!;
        var taps = new List<DetectedTap>();
        var position = 0;

        // The device closest to TDO is read first. A one marks the LSB of a 32 bit IDCODE,
        // a zero is the single bit of a bypass register.
        for (var device = 0; device < deviceCount; device++)
        {
            if (position >= tdo.Length)
                return OperationResult<IReadOnlyList<DetectedTap>>.Failure("Could not read the device IDCODEs.");

            if (!tdo[position])
            {
                taps.Add(new DetectedTap(null));
                position++;
                continue;
            }

            if (position + 32 > tdo.Length)
                return OperationResult<IReadOnlyList<DetectedTap>>.Failure("Could not read the device IDCODEs.");

            uint idCode = 0;
            for (var b = 0; b < 32; b++)
            {
                if (tdo[position + b])
                    idCode |= 1u << b;
            }

            taps.Add(new DetectedTap(idCode));
            position += 32;
        }

        taps.Reverse();
        return OperationResult<IReadOnlyList<DetectedTap>>.Success(taps);
    }
}