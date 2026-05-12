using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ATLab.Models;

namespace ATLab.Helpers;

public static class CommandCompiler
{
    private static readonly Regex VarRegex =
        new(@"\{([A-Za-z0-9_]+)\}", RegexOptions.Compiled);
    
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
        
        var blocks = new List<Block> { new(BlockType.ENCODED, expression, "math") };

        ResolveVariables(blocks, vars);
        EvaluateInlineMath(blocks);

        return blocks[0].Value;
    }
    
    private static List<Block> CompileInternal(string command, IEnumerable<CustomVariable>? vars)
    {
        var blocks = Tokenize(command);
        
        ResolveVariables(blocks, vars);
        
        EvaluateInlineMath(blocks);
        
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

                var text = input.Substring(i + 1, end - i - 1);
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
                    // {enc:content} (enc can be hex/bin/base64/ascii/upper/lower/len/bytes/whatever)
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

    private static void EvaluateInlineMath(List<Block> blocks)
    {
        foreach (var block in blocks)
        {
            if (block.Encoding != "math")
                continue;

            var expr = block.Value;

            try
            {
                // Normalize decimal separator to dot for internal math evaluation
                expr = expr.Replace(
                    CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator,
                    ".");
                
                // Also ensure we handle cases where a dot might already be present regardless of culture
                // (e.g. user manually typed a dot on a comma system, or variables from different sources)
                // However, dot is the target separator.

                expr = EnsureDoublePrecision(expr);

                var result = EvaluateMathExpression(expr);
                block.Value = result.ToString(CultureInfo.CurrentCulture);
            }
            catch
            {
                // if math fails, keep original block value
            }
        }
    }

    private static string EnsureDoublePrecision(string expr)
    {
        return Regex.Replace(expr, @"(?<![\d\.])(\d+)(?![\d\.])", "$1.0");
    }

    private static double EvaluateMathExpression(string expr)
    {
        var dt = new DataTable { Locale = CultureInfo.InvariantCulture };
        var result = dt.Compute(expr, "");
        return Convert.ToDouble(result, CultureInfo.InvariantCulture);
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
                "bytes"  => ConvertToByteString(v),
                "base64" => Convert.ToBase64String(Encoding.UTF8.GetBytes(v)),
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

                case "base64":
                    output.AddRange(Convert.FromBase64String(block.Value));
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

    private static string ConvertToByteString(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return "[" + string.Join(", ", bytes) + "]";
    }
    
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