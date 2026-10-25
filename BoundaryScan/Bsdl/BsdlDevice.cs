using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.Enums;

namespace ATLab.BoundaryScan.Bsdl;

public enum BsdlPortDirection
{
    In,
    Out,
    InOut,
    Buffer,
    Linkage
}

public enum BsdlCellFunction
{
    Input,
    Output2,
    Output3,
    Control,
    ControlR,
    Internal,
    Clock,
    Bidir,
    ObserveOnly,
    Unknown
}

/// <param name="Name">Name of a scalar port or of a single vector element, e.g. "PA(3)".</param>
public record BsdlPort(string Name, BsdlPortDirection Direction);

/// <param name="Port">Port element the cell belongs to, null for "*".</param>
/// <param name="SafeValue">Null when the safe value is "X".</param>
/// <param name="ControlCell">Control cell of an OUTPUT3 / BIDIR cell.</param>
/// <param name="DisableValue">Value of the control cell that disables the output.</param>
/// <param name="DisableResult">Pin state while disabled, e.g. "Z", "WEAK1", "PULL0".</param>
public record BsdlCell(
    int Number,
    string CellType,
    string? Port,
    BsdlCellFunction Function,
    bool? SafeValue,
    int? ControlCell,
    bool? DisableValue,
    string? DisableResult);

/// <param name="Port">Port element connected to the pin.</param>
public record BsdlPin(string PinName, string Port);

/// <param name="Input">Cell that captures the pin level.</param>
/// <param name="Output">Cell that drives the pin, its control cell enables the driver.</param>
public record BsdlPortCells(BsdlCell? Input, BsdlCell? Output);

public class BsdlDevice
{
    public string EntityName { get; init; } = string.Empty;
    public string DefaultPackage { get; init; } = string.Empty;
    public int InstructionLength { get; init; }

    /// <summary>Opcodes as written in the BSDL file, MSB first. Patterns may contain X.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Instructions { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>32 characters, MSB first, X marks bits that are ignored when comparing.</summary>
    public string? IdCodePattern { get; init; }

    public int BoundaryLength { get; init; }
    public IReadOnlyList<BsdlCell> BoundaryCells { get; init; } = [];

    public IReadOnlyList<BsdlPort> Ports { get; init; } = [];

    /// <summary>Package name to pin assignment.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<BsdlPin>> Packages { get; init; } =
        new Dictionary<string, IReadOnlyList<BsdlPin>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Ports of the test access port itself (TCK, TMS, TDI, TDO, TRST).</summary>
    public IReadOnlySet<string> TapPorts { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> Warnings { get; init; } = [];

    private IReadOnlyDictionary<string, BsdlPortCells>? _portCells;

    /// <summary>Input and output cell of every port that has boundary cells.</summary>
    public IReadOnlyDictionary<string, BsdlPortCells> PortCells => _portCells ??= ResolvePortCells();

    public BsdlPortCells GetPortCells(string port) =>
        PortCells.TryGetValue(port, out var cells) ? cells : new BsdlPortCells(null, null);

    public bool HasInstruction(string name) => Instructions.ContainsKey(name);

    /// <summary>Boundary register content with every cell at its safe value, X is taken as 0.</summary>
    public bool[] CreateSafeOutputs()
    {
        var outputs = new bool[BoundaryLength];

        foreach (var cell in BoundaryCells)
            outputs[cell.Number] = cell.SafeValue ?? false;

        return outputs;
    }

    /// <summary>
    /// Sets the output and control cell of a port. Returns false if the port has no output cell,
    /// or no control cell when it should be disabled.
    /// </summary>
    public static bool ApplyDrive(bool[] outputs, BsdlCell outputCell, PinDriveState state)
    {
        if (state == PinDriveState.HIGH_Z)
        {
            if (outputCell.ControlCell is not { } disableCell)
                return false;

            outputs[disableCell] = outputCell.DisableValue ?? true;
            return true;
        }

        outputs[outputCell.Number] = state == PinDriveState.HIGH;

        // Control cells are often shared by several outputs, enabling one enables all of them
        if (outputCell.ControlCell is { } controlCell)
            outputs[controlCell] = !(outputCell.DisableValue ?? true);

        return true;
    }

    private Dictionary<string, BsdlPortCells> ResolvePortCells()
    {
        var inputCells = new Dictionary<string, BsdlCell>(StringComparer.OrdinalIgnoreCase);
        var outputCells = new Dictionary<string, BsdlCell>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in BoundaryCells.Where(c => c.Port != null))
        {
            switch (cell.Function)
            {
                case BsdlCellFunction.Input or BsdlCellFunction.Clock or BsdlCellFunction.ObserveOnly:
                    inputCells.TryAdd(cell.Port!, cell);
                    break;
                case BsdlCellFunction.Output2 or BsdlCellFunction.Output3:
                    outputCells.TryAdd(cell.Port!, cell);
                    break;
                case BsdlCellFunction.Bidir:
                    inputCells.TryAdd(cell.Port!, cell);
                    outputCells.TryAdd(cell.Port!, cell);
                    break;
            }
        }

        return inputCells.Keys.Union(outputCells.Keys, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                port => port,
                port => new BsdlPortCells(inputCells.GetValueOrDefault(port), outputCells.GetValueOrDefault(port)),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the opcode as LSB first bit array ready to be shifted. X bits are replaced by zero.
    /// </summary>
    public bool[]? GetOpcode(string name)
    {
        if (!Instructions.TryGetValue(name, out var patterns) || patterns.Count == 0)
            return null;

        return patterns[0].Reverse().Select(c => c == '1').ToArray();
    }

    public bool MatchesIdCode(uint idCode)
    {
        if (IdCodePattern is not { Length: 32 } pattern)
            return false;

        for (var bit = 0; bit < 32; bit++)
        {
            var expected = pattern[31 - bit];
            if (expected is 'X' or 'x')
                continue;

            if (((idCode >> bit) & 1) != (expected == '1' ? 1u : 0u))
                return false;
        }

        return true;
    }
}