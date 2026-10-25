using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.BoundaryScan.Buses;
using ATLab.Models;

namespace ATLab.Interfaces;

/// <param name="HasChecks">False for steps that only drive outputs or turn boundary scan off.</param>
/// <param name="Mismatches">Number of expected pin levels that were not met.</param>
/// <param name="Messages">One line per mismatch, e.g. "U3 IO(5) (FAULT_N): expected 0, read 1".</param>
public record BoundaryScanStepOutcome(bool HasChecks, int Mismatches, IReadOnlyList<string> Messages);

/// <summary>
/// Boundary scan state of a test run. The first boundary scan step connects the probe and verifies the chain,
/// pin states then persist across steps until the session ends.
/// </summary>
public interface IBoundaryScanTestSession
{
    bool IsActive { get; }

    event Action? ActiveChanged;

    Task<OperationResult<BoundaryScanStepOutcome>> ExecuteStepAsync(BoundaryScanStepConfig step, CancellationToken token);

    /// <summary>Runs one transaction on a bus of the Boundary Scan tab, all edges in a single probe transfer.</summary>
    Task<OperationResult<BusTransactionResult>> ExecuteBusTransactionAsync(BoundaryScanBusStepConfig step, byte[] write, CancellationToken token);

    /// <summary>Resets the chips to normal operation and releases the probe. Does nothing when no session is active.</summary>
    Task EndAsync();
}