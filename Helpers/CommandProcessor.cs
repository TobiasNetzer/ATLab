using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ATLab.Models;

namespace ATLab.Helpers;

/// <summary>
/// Turns command texts into bytes or strings: resolves {Variable} references, evaluates {expr:...} blocks and
/// applies encodings such as {hex:...} or {u16be:...}.
/// </summary>
public static class CommandProcessor
{
    private static readonly Regex VarRegex =
        new(@"\{([A-Za-z0-9_.]+)\}", RegexOptions.Compiled);

    // {u16be:…}, {i8:…}, {f32le:…}: number as raw binary, multi-byte without suffix = big-endian
    private static readonly Regex NumericEncodingRegex =
        new(@"^(?<type>[uif])(?<bits>\d+)(?<order>be|le)?$", RegexOptions.Compiled);
    
    /// <summary>Compiles a command for sending: variables first, then expressions, then encodings.</summary>
    public static byte[] CompileToBytes(string command, IEnumerable<CustomVariable>? vars)
    {
        var blocks = CompileInternal(command, vars);
        return EncodeBlocks(blocks);
    }

    public static string CompileToString(string command, IEnumerable<CustomVariable>? vars)
    {
        var blocks = CompileInternal(command, vars);
        return string.Concat(blocks.Select(b => b.Value));
    }

    public static string EvaluateExpression(string expression, IEnumerable<CustomVariable>? vars)
    {
        if (string.IsNullOrWhiteSpace(expression)) return "0";
        
        var blocks = new List<Block> { new(BlockType.ENCODED, expression, "expr") };

        ResolveVariables(blocks, vars);
        EvaluateInlineExpression(blocks);

        return blocks[0].Value;
    }
    
    private static List<Block> CompileInternal(string command, IEnumerable<CustomVariable>? vars)
    {
        var blocks = Tokenize(command);
        
        ResolveVariables(blocks, vars);
        
        EvaluateInlineExpression(blocks);
        
        ApplyFormatting(blocks);

        return blocks;
    }
    
    private static List<Block> Tokenize(string input)
    {
        var blocks = new List<Block>();
        var i = 0;

        while (i < input.Length)
        {
            if (input[i] == '"')
            {
                var end = input.IndexOf('"', i + 1);
                if (end < 0) end = input.Length;

                var text = input.Substring(i, end - i + 1);
                blocks.Add(new Block(BlockType.ASCII, text, "ascii"));
                i = Math.Min(end + 1, input.Length);
            }
            else if (input[i] == '{')
            {
                var end = -1;
                var depth = 0;
                for (var j = i; j < input.Length; j++)
                {
                    if (input[j] == '{') depth++;
                    else if (input[j] == '}')
                    {
                        depth--;
                        if (depth != 0)
                            continue;
                        
                        end = j;
                        break;
                    }
                }

                if (end < 0)
                {
                    blocks.Add(new Block(BlockType.ASCII, input[i].ToString(), "ascii"));
                    i++;
                    continue;
                }

                var inner = input.Substring(i + 1, end - i - 1);
                var parts = inner.Split(':', 2);

                if (parts.Length == 2)
                {
                    // {enc:content} (enc can be hex/bin/ascii/upper/lower/len/expr or a numeric type like u16be)
                    blocks.Add(new Block(BlockType.ENCODED, parts[1].Trim(), parts[0].Trim().ToLowerInvariant()));
                }
                else
                {
                    // {Var} → preserve braces so VarRegex can match
                    var name = parts[0].Trim();
                    blocks.Add(new Block(BlockType.ENCODED, "{" + name + "}", "ascii"));
                }

                i = end + 1;
            }
            else
            {
                blocks.Add(new Block(BlockType.ASCII, input[i].ToString(), "ascii"));
                i++;
            }
        }

        return blocks;
    }
    
    private static void ResolveVariables(List<Block> blocks, IEnumerable<CustomVariable>? vars)
    {
        foreach (var block in blocks)
        {
            block.Value = VarRegex.Replace(block.Value, m =>
            {
                var name = m.Groups[1].Value;
                var variable = vars?.FirstOrDefault(v => v.Name == name);
                return variable?.Value ?? "0.0";
            });
        }
    }

    private static void EvaluateInlineExpression(List<Block> blocks)
    {
        foreach (var block in blocks)
        {
            if (block.Encoding != "expr" && GetNumericEncoding(block.Encoding) == null)
                continue;

            var expr = block.Value;

            try
            {
                // Normalize decimal separator to dot for internal math evaluation. ',' is always
                // accepted, independent of the culture, as it has no other meaning in expressions
                expr = expr
                    .Replace(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, ".")
                    .Replace(',', '.');

                var result = ExpressionEvaluator.Evaluate(expr);
                block.Value = result.ToString("G15", CultureInfo.InvariantCulture);
            }
            catch
            {
                // if math fails, keep original block value
            }
        }
    }

    private static void ApplyFormatting(List<Block> blocks)
    {
        foreach (var block in blocks)
        {
            if (block.Type != BlockType.ENCODED)
                continue;

            var v = block.Value;

            block.Value = block.Encoding switch
            {
                "ascii"  => v,
                "upper"  => v.ToUpperInvariant(),
                "lower"  => v.ToLowerInvariant(),
                "len"    => v.Length.ToString(CultureInfo.InvariantCulture),
                _        => v
            };
        }
    }
    
    private static byte[] EncodeBlocks(List<Block> blocks)
    {
        var output = new List<byte>();

        foreach (var block in blocks)
        {
            switch (block.Encoding)
            {
                case "hex":
                    output.AddRange(ParseHex(block.Value));
                    break;

                case "bin":
                    output.AddRange(ParseBinary(block.Value));
                    break;

                case var encoding when GetNumericEncoding(encoding) is { } numeric:
                    output.AddRange(EncodeNumber(block.Value, numeric, encoding));
                    break;

                default:
                    output.AddRange(Encoding.ASCII.GetBytes(block.Value));
                    break;
            }
        }

        return output.ToArray();
    }
    
    private static byte[] ParseHex(string hex)
    {
        hex = hex.Replace("0x", "", StringComparison.OrdinalIgnoreCase)
                 .Replace(" ", "")
                 .Replace("-", "");

        if (hex.Length % 2 != 0)
            hex = "0" + hex;

        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

        return bytes;
    }

    private static byte[] ParseBinary(string bin)
    {
        bin = bin.Replace(" ", "");

        if (bin.Length % 8 != 0)
            bin = bin.PadLeft(((bin.Length / 8) + 1) * 8, '0');

        var bytes = new byte[bin.Length / 8];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(bin.Substring(i * 8, 8), 2);

        return bytes;
    }

    private static NumericEncoding? GetNumericEncoding(string encoding)
    {
        var match = NumericEncodingRegex.Match(encoding);
        if (!match.Success)
            return null;

        var type = match.Groups["type"].Value[0];
        var bits = int.Parse(match.Groups["bits"].Value, CultureInfo.InvariantCulture);

        var supported = type == 'f'
            ? bits == 32
            : bits is 8 or 16 or 32;

        if (!supported)
            throw new FormatException(
                $"Unknown encoding '{encoding}'. Supported: u8, i8, u16, i16, u32, i32, f32 (with optional be/le suffix).");

        return new NumericEncoding(type, bits, match.Groups["order"].Value != "le");
    }

    private static byte[] EncodeNumber(string value, NumericEncoding numeric, string encoding)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            throw new FormatException($"{{{encoding}:…}}: '{value}' is not a number.");

        var bytes = new byte[numeric.Bits / 8];

        if (numeric.Type == 'f')
        {
            if (numeric.BigEndian)
                BinaryPrimitives.WriteSingleBigEndian(bytes, (float)number);
            else
                BinaryPrimitives.WriteSingleLittleEndian(bytes, (float)number);

            return bytes;
        }

        var rounded = Math.Round(number, MidpointRounding.AwayFromZero);
        var min = numeric.Type == 'i' ? -Math.Pow(2, numeric.Bits - 1) : 0;
        var max = numeric.Type == 'i' ? Math.Pow(2, numeric.Bits - 1) - 1 : Math.Pow(2, numeric.Bits) - 1;

        if (!double.IsFinite(rounded) || rounded < min || rounded > max)
            throw new FormatException(
                $"{{{encoding}:…}}: {value} is out of range ({min.ToString(CultureInfo.InvariantCulture)} … {max.ToString(CultureInfo.InvariantCulture)}).");

        // two's complement for negative values
        var raw = (ulong)(long)rounded;

        for (var b = 0; b < bytes.Length; b++)
        {
            var shift = 8 * (numeric.BigEndian ? bytes.Length - 1 - b : b);
            bytes[b] = (byte)(raw >> shift);
        }

        return bytes;
    }

    private sealed record NumericEncoding(char Type, int Bits, bool BigEndian);

    private enum BlockType { ASCII, ENCODED }

    private sealed class Block
    {
        public BlockType Type { get; }
        public string Encoding { get; }
        public string Value { get; set; }

        public Block(BlockType type, string value, string encoding)
        {
            Type = type;
            Value = value;
            Encoding = encoding;
        }
    }
}