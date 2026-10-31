using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ATLab.Models;

namespace ATLab.BoundaryScan.Bsdl;

/// <summary>
/// Reads the parts of an IEEE 1149.1 BSDL file needed for interactive boundary scan:
/// ports, pin maps, instruction opcodes, IDCODE and the boundary register.
/// </summary>
public static partial class BsdlParser
{
    public static OperationResult<BsdlDevice> ParseFile(string path)
    {
        try
        {
            return Parse(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            return OperationResult<BsdlDevice>.Failure($"Could not read BSDL file '{Path.GetFileName(path)}': {ex.Message}");
        }
    }

    public static OperationResult<BsdlDevice> Parse(string text)
    {
        try
        {
            return OperationResult<BsdlDevice>.Success(ParseInternal(StripComments(text)));
        }
        catch (FormatException ex)
        {
            return OperationResult<BsdlDevice>.Failure($"Invalid BSDL file: {ex.Message}");
        }
    }

    private static BsdlDevice ParseInternal(string text)
    {
        var warnings = new List<string>();

        var entity = EntityRegex().Match(text);
        if (!entity.Success)
            throw new FormatException("No entity declaration found.");

        var defaultPackage = PhysicalPinMapRegex().Match(text) is { Success: true } physical
            ? physical.Groups[1].Value.Trim()
            : string.Empty;

        var ports = ParsePorts(text);

        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in AttributeRegex().Matches(text))
            attributes.TryAdd(match.Groups[1].Value, match.Groups[2].Value);

        var instructionLength = ReadInteger(attributes, "INSTRUCTION_LENGTH")
                                ?? throw new FormatException("INSTRUCTION_LENGTH is missing.");

        var instructions = ParseInstructions(ReadString(attributes, "INSTRUCTION_OPCODE")
                                             ?? throw new FormatException("INSTRUCTION_OPCODE is missing."));

        foreach (var (name, patterns) in instructions)
        {
            if (patterns.Any(p => p.Length != instructionLength))
                warnings.Add($"Opcode of {name} does not match the instruction length {instructionLength}.");
        }

        foreach (var required in new[] { "BYPASS", "EXTEST", "SAMPLE" })
        {
            if (!instructions.ContainsKey(required))
                warnings.Add($"Instruction {required} is not defined.");
        }

        var idCode = ReadString(attributes, "IDCODE_REGISTER");
        if (idCode != null)
        {
            idCode = new string(idCode.Where(c => !char.IsWhiteSpace(c)).ToArray());
            if (idCode.Length != 32)
            {
                warnings.Add("IDCODE_REGISTER is not 32 bits long and is ignored.");
                idCode = null;
            }
        }

        var boundaryLength = ReadInteger(attributes, "BOUNDARY_LENGTH") ?? 0;
        var cells = ParseBoundaryRegister(ReadString(attributes, "BOUNDARY_REGISTER") ?? string.Empty);

        if (boundaryLength == 0)
            boundaryLength = cells.Count == 0 ? 0 : cells.Max(c => c.Number) + 1;

        if (cells.Any(c => c.Number >= boundaryLength))
            throw new FormatException("BOUNDARY_REGISTER contains cells beyond BOUNDARY_LENGTH.");

        if (cells.Count != boundaryLength)
            warnings.Add($"BOUNDARY_REGISTER describes {cells.Count} of {boundaryLength} cells.");

        var packages = ParsePackages(text, ports);
        if (packages.Count == 0)
            warnings.Add("No package pin map found.");

        var tapPorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in TapScanRegex().Matches(text))
            tapPorts.Add(NormalizePortName(match.Groups[1].Value));

        return new BsdlDevice
        {
            EntityName = entity.Groups[1].Value,
            DefaultPackage = defaultPackage,
            InstructionLength = instructionLength,
            Instructions = instructions,
            IdCodePattern = idCode,
            BoundaryLength = boundaryLength,
            BoundaryCells = cells,
            Ports = ports,
            Packages = packages,
            TapPorts = tapPorts,
            Warnings = warnings
        };
    }

    private static string StripComments(string text)
    {
        var builder = new StringBuilder(text.Length);
        var inString = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (c == '"')
                inString = !inString;

            if (c is '\n' or '\r')
                inString = false;

            if (!inString && c == '-' && i + 1 < text.Length && text[i + 1] == '-')
            {
                while (i < text.Length && text[i] != '\n')
                    i++;

                builder.Append('\n');
                continue;
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    private static List<BsdlPort> ParsePorts(string text)
    {
        var start = PortStartRegex().Match(text);
        if (!start.Success)
            throw new FormatException("No port declaration found.");

        // The port list contains parentheses itself, so the end is found by balancing them
        var depth = 1;
        var index = start.Index + start.Length;
        var begin = index;

        while (index < text.Length && depth > 0)
        {
            if (text[index] == '(') depth++;
            else if (text[index] == ')') depth--;
            index++;
        }

        if (depth != 0)
            throw new FormatException("Unterminated port declaration.");

        var ports = new List<BsdlPort>();

        foreach (var declaration in text[begin..(index - 1)].Split(';'))
        {
            var match = PortDeclarationRegex().Match(declaration);
            if (!match.Success)
                continue;

            var direction = match.Groups["dir"].Value.ToLowerInvariant() switch
            {
                "in" => BsdlPortDirection.In,
                "out" => BsdlPortDirection.Out,
                "inout" => BsdlPortDirection.InOut,
                "buffer" => BsdlPortDirection.Buffer,
                _ => BsdlPortDirection.Linkage
            };

            var names = match.Groups["names"].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var name in names)
            {
                if (match.Groups["from"].Success)
                {
                    foreach (var element in VectorRange(match))
                        ports.Add(new BsdlPort($"{name.ToUpperInvariant()}({element})", direction));
                }
                else
                {
                    ports.Add(new BsdlPort(name.ToUpperInvariant(), direction));
                }
            }
        }

        if (ports.Count == 0)
            throw new FormatException("The port declaration is empty.");

        return ports;
    }

    // Vector elements in declaration order, which is also the order of the pins in a pin map
    private static IEnumerable<int> VectorRange(Match match)
    {
        var from = int.Parse(match.Groups["from"].Value, CultureInfo.InvariantCulture);
        var to = int.Parse(match.Groups["to"].Value, CultureInfo.InvariantCulture);
        var step = from <= to ? 1 : -1;

        for (var i = from; i != to + step; i += step)
            yield return i;
    }

    private static Dictionary<string, IReadOnlyList<string>> ParseInstructions(string opcodes)
    {
        var instructions = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in OpcodeRegex().Matches(opcodes))
        {
            var patterns = match.Groups[2].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            instructions[match.Groups[1].Value.ToUpperInvariant()] = patterns;
        }

        if (instructions.Count == 0)
            throw new FormatException("INSTRUCTION_OPCODE does not contain any instruction.");

        return instructions;
    }

    private static List<BsdlCell> ParseBoundaryRegister(string register)
    {
        var cells = new List<BsdlCell>();

        foreach (Match match in CellRegex().Matches(register))
        {
            var fields = match.Groups["fields"].Value
                .Split(',', StringSplitOptions.TrimEntries);

            if (fields.Length < 4)
                throw new FormatException($"Boundary cell {match.Groups["num"].Value} is incomplete.");

            var port = fields[1] == "*" ? null : NormalizePortName(fields[1]);

            cells.Add(new BsdlCell(
                int.Parse(match.Groups["num"].Value, CultureInfo.InvariantCulture),
                fields[0].ToUpperInvariant(),
                port,
                ParseFunction(fields[2]),
                ParseBit(fields[3]),
                fields.Length > 4 && int.TryParse(fields[4], out var control) ? control : null,
                fields.Length > 5 ? ParseBit(fields[5]) : null,
                fields.Length > 6 ? fields[6].ToUpperInvariant() : null));
        }

        return cells.OrderBy(c => c.Number).ToList();
    }

    private static BsdlCellFunction ParseFunction(string function) => function.ToUpperInvariant() switch
    {
        "INPUT" => BsdlCellFunction.Input,
        "OUTPUT2" => BsdlCellFunction.Output2,
        "OUTPUT3" => BsdlCellFunction.Output3,
        "CONTROL" => BsdlCellFunction.Control,
        "CONTROLR" => BsdlCellFunction.ControlR,
        "INTERNAL" => BsdlCellFunction.Internal,
        "CLOCK" => BsdlCellFunction.Clock,
        "BIDIR" => BsdlCellFunction.Bidir,
        "OBSERVE_ONLY" => BsdlCellFunction.ObserveOnly,
        _ => BsdlCellFunction.Unknown
    };

    private static bool? ParseBit(string value) => value.Trim() switch
    {
        "0" => false,
        "1" => true,
        _ => null
    };

    private static Dictionary<string, IReadOnlyList<BsdlPin>> ParsePackages(string text, List<BsdlPort> ports)
    {
        var packages = new Dictionary<string, IReadOnlyList<BsdlPin>>(StringComparer.OrdinalIgnoreCase);

        // Vector ports are listed by their base name, the pins follow the declared range order
        var vectorElements = ports
            .Where(p => p.Name.EndsWith(')'))
            .GroupBy(p => p.Name[..p.Name.IndexOf('(')], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(p => p.Name).ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (Match match in PinMapConstantRegex().Matches(text))
        {
            var map = ConcatenateStrings(match.Groups[2].Value);
            var pins = new List<BsdlPin>();

            foreach (Match entry in PinMapEntryRegex().Matches(map))
            {
                var port = entry.Groups["port"].Value.Trim().ToUpperInvariant();
                var pinNames = (entry.Groups["list"].Success ? entry.Groups["list"].Value : entry.Groups["pin"].Value)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (vectorElements.TryGetValue(port, out var elements))
                {
                    for (var i = 0; i < Math.Min(elements.Count, pinNames.Length); i++)
                        pins.Add(new BsdlPin(pinNames[i], elements[i]));
                }
                else
                {
                    foreach (var pinName in pinNames)
                        pins.Add(new BsdlPin(pinName, port));
                }
            }

            packages[match.Groups[1].Value] = pins;
        }

        return packages;
    }

    private static string NormalizePortName(string port) =>
        new string(port.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();

    private static string? ReadString(Dictionary<string, string> attributes, string name) =>
        attributes.TryGetValue(name, out var value) ? ConcatenateStrings(value) : null;

    private static int? ReadInteger(Dictionary<string, string> attributes, string name) =>
        attributes.TryGetValue(name, out var value) &&
        int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;

    // BSDL splits long strings into literals joined with '&'
    private static string ConcatenateStrings(string value)
    {
        var literals = StringLiteralRegex().Matches(value);
        return literals.Count == 0
            ? value.Trim()
            : string.Concat(literals.Select(m => m.Groups[1].Value));
    }

    [GeneratedRegex(@"\bentity\s+(\w+)\s+is\b", RegexOptions.IgnoreCase)]
    private static partial Regex EntityRegex();

    [GeneratedRegex(@"\bPHYSICAL_PIN_MAP\s*:\s*string\s*:=\s*""([^""]*)""", RegexOptions.IgnoreCase)]
    private static partial Regex PhysicalPinMapRegex();

    [GeneratedRegex(@"\bport\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex PortStartRegex();

    [GeneratedRegex(@"^\s*(?<names>[\w\s,]+?)\s*:\s*(?<dir>in|out|inout|buffer|linkage)\s+(?:bit_vector\s*\(\s*(?<from>\d+)\s+(?:to|downto)\s+(?<to>\d+)\s*\)|bit)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex PortDeclarationRegex();

    [GeneratedRegex(@"\battribute\s+(\w+)\s+of\s+[\w]+\s*:\s*\w+\s+is\s+((?:""[^""]*""|[^;""])*);", RegexOptions.IgnoreCase)]
    private static partial Regex AttributeRegex();

    [GeneratedRegex(@"\bconstant\s+(\w+)\s*:\s*PIN_MAP_STRING\s*:=\s*((?:""[^""]*""|[^;""])*);", RegexOptions.IgnoreCase)]
    private static partial Regex PinMapConstantRegex();

    [GeneratedRegex(@"(?<port>[\w]+)\s*:\s*(?:\((?<list>[^)]*)\)|(?<pin>[\w]+))")]
    private static partial Regex PinMapEntryRegex();

    [GeneratedRegex(@"(\w+)\s*\(([^)]*)\)")]
    private static partial Regex OpcodeRegex();

    // The port field may contain a vector index, e.g. "PA(3)", so the field list is matched with one nested level
    [GeneratedRegex(@"(?<num>\d+)\s*\(\s*(?<fields>(?:[^()]|\([^()]*\))*)\)")]
    private static partial Regex CellRegex();

    [GeneratedRegex(@"\battribute\s+TAP_SCAN_\w+\s+of\s+(\w+(?:\s*\(\s*\d+\s*\))?)\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex TapScanRegex();

    [GeneratedRegex(@"""([^""]*)""")]
    private static partial Regex StringLiteralRegex();
}