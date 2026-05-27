using ATLab.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ATLab.Enums;

namespace ATLab.Helpers;

public static class ResponseProcessor
{
    public static string Process(byte[] input, ResponseMask? mask)
    {
        if (mask == null)
            return Encoding.ASCII.GetString(input);
        
        var resultString = "0";

        mask.Result = "";
        mask.ProcessedInput = "";

        mask.RawOriginal = input.ToArray();
        mask.OriginalResponse = Format(mask.RawOriginal, mask.ResponseDisplayMode);

        var processedInput = ApplySkipAndLength(input, mask.Skip, mask.Length);

        mask.RawProcessed = processedInput.ToArray();
        mask.ProcessedInput = Format(mask.RawProcessed, mask.ResponseDisplayMode);

        if (processedInput.Length == 0)
            return string.Empty;

        if (string.IsNullOrWhiteSpace(mask.Mask))
        {
            mask.Result = resultString;
            return resultString;
        }

        var parsed = MaskParser.Parse(mask.Mask);

        var ascii = Encoding.ASCII.GetString(processedInput);

        var asciiCursor = 0;
        var byteCursor = 0;

        var matched = true;

        foreach (var rule in parsed.MatchRules)
        {
            switch (rule)
            {
                case AsciiMatchRule ar when !ar.IsMatch(ascii, ref asciiCursor):

                case ByteSequenceMatchRule br when !br.IsMatch(processedInput, ref byteCursor):
                    matched = false;
                    break;
            }

            if (!matched)
                break;
        }

        if (!matched)
        {
            resultString = "0";
        }
        else if (parsed.NumericExtraction != null)
        {
            var result = ExtractNumeric(processedInput, ascii, parsed.NumericExtraction);

            resultString = result?.ToString(CultureInfo.InvariantCulture) ?? "0";
        }
        else if (parsed.MatchRules.Count == 0)
        {
            resultString = "0";
        }
        else
        {
            resultString = "1";
        }

        mask.Result = resultString;
        return resultString;
    }
    
    public static string Format(byte[]? data, ResponseDisplayMode mode)
    {
        if (data == null)
            return string.Empty;

        return mode switch
        {
            ResponseDisplayMode.ASCII => Encoding.ASCII.GetString(data),
            ResponseDisplayMode.HEX   => BitConverter.ToString(data).Replace("-", " "),
            _ => throw new NotSupportedException()
        };
    }

    private static byte[] ApplySkipAndLength(byte[] input, int skip, int length)
    {
        var processed = input;

        if (skip > 0)
        {
            processed = skip < processed.Length
                ? processed.Skip(skip).ToArray()
                : Array.Empty<byte>();
        }

        if (length > 0 && length < processed.Length)
        {
            processed = processed.Take(length).ToArray();
        }

        return processed;
    }

    private static double? ExtractNumeric(byte[] input, string ascii, NumericExtractionSpec spec)
    {
        switch (spec.Type)
        {
            case NumericExtractionType.BYTE:
            {
                if (input.Length < spec.ByteCount)
                    return null;

                var slice = new byte[spec.ByteCount];
                Array.Copy(input, 0, slice, 0, spec.ByteCount);

                if (spec.Endian == Endianness.LSB)
                    Array.Reverse(slice);

                return slice.Aggregate(0, (current, b) => (current << 8) | b);
            }

            case NumericExtractionType.HEX_ASCII:
            {
                if (ascii.Length == 0)
                    return null;

                try
                {
                    return int.Parse(ascii, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }
                catch
                {
                    return null;
                }
            }

            case NumericExtractionType.BIN_ASCII:
            {
                if (ascii.Length == 0)
                    return null;

                try
                {
                    return Convert.ToInt32(ascii, 2);
                }
                catch
                {
                    return null;
                }
            }

            case NumericExtractionType.DEC_ASCII:
            {
                if (ascii.Length == 0)
                    return null;

                try
                {
                    return int.Parse(ascii, CultureInfo.InvariantCulture);
                }
                catch
                {
                    return null;
                }
            }
            case NumericExtractionType.FLOAT_ASCII:
            {
                if (ascii.Length == 0)
                    return null;

                if (double.TryParse(ascii, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                    return Convert.ToDouble(f);
                
                return null;
            }
            case NumericExtractionType.ASCII:
            default:
                if (ascii.Length == 0)
                    return null;

                var bytes = Encoding.ASCII.GetBytes(ascii);

                return bytes.Aggregate(0, (current, b) => (current << 8) | b);
        }
    }
}

#region Mask parsing and rules

internal enum NumericExtractionType
{
    BYTE,
    ASCII,
    HEX_ASCII,
    BIN_ASCII,
    DEC_ASCII,
    FLOAT_ASCII
}

internal enum Endianness
{
    MSB,
    LSB
}

internal sealed class NumericExtractionSpec
{
    public NumericExtractionType Type { get; }
    public int ByteCount { get; }
    public Endianness Endian { get; }

    public NumericExtractionSpec(NumericExtractionType type, int byteCount = 0, Endianness endian = Endianness.MSB)
    {
        Type = type;
        ByteCount = byteCount;
        Endian = endian;
    }
}

internal interface IMatchRule { }

internal sealed class AsciiMatchRule : IMatchRule
{
    private readonly string _needle;

    public AsciiMatchRule(string needle)
    {
        _needle = needle;
    }

    public bool IsMatch(string ascii, ref int cursor)
    {
        var index = ascii.IndexOf(_needle, cursor, StringComparison.Ordinal);
        if (index < 0)
            return false;

        cursor = index + _needle.Length;
        return true;
    }
}

internal sealed class ByteSequenceMatchRule : IMatchRule
{
    private readonly byte[] _sequence;

    public ByteSequenceMatchRule(byte[] sequence)
    {
        _sequence = sequence;
    }

    public bool IsMatch(byte[] input, ref int cursor)
    {
        for (var i = cursor; i <= input.Length - _sequence.Length; i++)
        {
            var match = !_sequence
                .Where((t, j) => input[i + j] != t)
                .Any();

            if (!match) 
                continue;
            
            cursor = i + _sequence.Length;
            return true;
        }

        return false;
    }
}

internal sealed class ParsedMask
{
    public List<IMatchRule> MatchRules { get; } = new();
    public NumericExtractionSpec? NumericExtraction { get; set; }
}

internal static class MaskParser
{
    public static ParsedMask Parse(string pattern)
    {
        var result = new ParsedMask();
        var i = 0;

        while (i < pattern.Length)
        {
            SkipWhitespace(pattern, ref i);
            if (i >= pattern.Length)
                break;

            var c = pattern[i];

            switch (c)
            {
                case '"':
                {
                    var literal = ParseQuotedString(pattern, ref i);
                    result.MatchRules.Add(new AsciiMatchRule(literal));
                    break;
                }
                case '0' when i + 1 < pattern.Length && (pattern[i + 1] == 'x' || pattern[i + 1] == 'X'):
                {
                    var bytes = ParseHexBytes(pattern, ref i);
                    result.MatchRules.Add(new ByteSequenceMatchRule(bytes));
                    break;
                }
                case '{':
                {
                    var spec = ParseNumericExtraction(pattern, ref i);
                    result.NumericExtraction = spec;
                    break;
                }
                default:
                    i++;
                    break;
            }
        }

        return result;
    }

    private static void SkipWhitespace(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i]))
            i++;
    }

    private static string ParseQuotedString(string s, ref int i)
    {
        i++;
        var start = i;

        while (i < s.Length && s[i] != '"')
            i++;

        var literal = s.Substring(start, i - start);

        if (i < s.Length && s[i] == '"')
            i++;

        return literal;
    }

    private static byte[] ParseHexBytes(string s, ref int i)
    {
        i += 2;

        var start = i;
        while (i < s.Length && IsHexChar(s[i]))
            i++;

        var hex = s.Substring(start, i - start);

        if (hex.Length % 2 != 0)
            hex = "0" + hex;

        var bytes = new byte[hex.Length / 2];

        for (var b = 0; b < bytes.Length; b++)
        {
            bytes[b] = byte.Parse(hex.AsSpan(b * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return bytes;
    }

    private static bool IsHexChar(char c)
    {
        return (c >= '0' && c <= '9')
               || (c >= 'a' && c <= 'f')
               || (c >= 'A' && c <= 'F');
    }

    private static NumericExtractionSpec ParseNumericExtraction(string s, ref int i)
    {
        i++;
        var start = i;

        while (i < s.Length && s[i] != '}')
            i++;

        var content = s.Substring(start, i - start);

        if (i < s.Length && s[i] == '}')
            i++;

        if (!content.StartsWith("byte:", StringComparison.OrdinalIgnoreCase))
            return content.ToLowerInvariant() switch
            {
                "ascii" => new NumericExtractionSpec(NumericExtractionType.ASCII),
                "hex" => new NumericExtractionSpec(NumericExtractionType.HEX_ASCII),
                "bin" => new NumericExtractionSpec(NumericExtractionType.BIN_ASCII),
                "dec" => new NumericExtractionSpec(NumericExtractionType.DEC_ASCII),
                "float" => new NumericExtractionSpec(NumericExtractionType.FLOAT_ASCII),
                _ => throw new FormatException($"Unknown numeric extraction: {{{content}}}")
            };
        var parts = content.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var fmtAndCount = parts[0].Split(':');
        var byteCount = int.Parse(fmtAndCount[1], CultureInfo.InvariantCulture);

        var endian = Endianness.MSB;

        if (parts.Length > 1)
        {
            endian = parts[1].ToLowerInvariant() switch
            {
                "msb" => Endianness.MSB,
                "lsb" => Endianness.LSB,
                _ => throw new FormatException($"Unknown endianness '{parts[1]}'")
            };
        }

        return new NumericExtractionSpec(NumericExtractionType.BYTE, byteCount, endian);
    }
}

#endregion