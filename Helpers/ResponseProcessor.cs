using ATLab.Models;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ATLab.Enums;

namespace ATLab.Helpers;

public sealed class ResponseProcessingException : Exception
{
    public ResponseProcessingException(string message) : base(message) { }
}

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

        var skip = Math.Max(mask.Skip, 0);

        if (mask.Length > 0 && processedInput.Length < mask.Length)
            throw new ResponseProcessingException(
                $"Response too short: expected {mask.Length} bytes after skipping {skip}, " +
                $"but only {processedInput.Length} available ({input.Length} bytes received).");

        if (processedInput.Length == 0)
            throw new ResponseProcessingException(input.Length == 0
                ? "Response is empty."
                : $"Response too short: no data left after skipping {skip} of {input.Length} received bytes.");

        // empty mask: parse the processed data as a number, like a step without a custom mask
        var maskText = string.IsNullOrWhiteSpace(mask.Mask) ? "{float}" : mask.Mask;

        ParsedMask parsed;
        try
        {
            parsed = MaskParser.Parse(maskText);
        }
        catch (FormatException ex)
        {
            throw new ResponseProcessingException($"Invalid mask '{mask.Mask}': {ex.Message}");
        }

        var ascii = Encoding.ASCII.GetString(processedInput);

        var asciiCursor = 0;
        var byteCursor = 0;

        var matched = true;

        // position right after the match rule preceding the extraction; ASCII decoding maps one byte to one
        // char, so text and byte cursors share the same positions
        int? extractionStart = null;

        for (var r = 0; r < parsed.MatchRules.Count; r++)
        {
            var rule = parsed.MatchRules[r];

            matched = rule switch
            {
                AsciiMatchRule ar => ar.IsMatch(ascii, ref asciiCursor),
                ByteSequenceMatchRule br => br.IsMatch(processedInput, ref byteCursor),
                _ => true
            };

            if (!matched)
                break;

            if (r == parsed.ExtractionAfterRule)
                extractionStart = rule is AsciiMatchRule ? asciiCursor : byteCursor;
        }

        if (!matched)
        {
            resultString = "0";
        }
        else if (parsed.NumericExtraction != null)
        {
            var spec = parsed.NumericExtraction;
            var isBinary = spec.Type is NumericExtractionType.BYTE or NumericExtractionType.FLOAT32;
            var shownData = isBinary
                ?Format(processedInput, ResponseDisplayMode.HEX)
                : ascii.Trim();

            double? result;

            if (extractionStart is { } start)
            {
                // extraction after a match rule: binary types read the bytes following the match,
                // text types read the number token following the match
                var remaining = processedInput[start..];
                result = ExtractNumeric(remaining, isBinary ? "" : ReadNumberToken(ascii[start..], spec.Type), spec);

                if (result == null)
                    throw new ResponseProcessingException(
                        $"Numeric extraction {spec} failed after the match at position {start} of processed data '{shownData}' ({processedInput.Length} bytes).");
            }
            else
            {
                result = ExtractNumeric(processedInput, ascii, spec)
                         ?? throw new ResponseProcessingException(
                             $"Numeric extraction {spec} failed on processed data '{shownData}' ({processedInput.Length} bytes).");
            }

            resultString = result.Value.ToString(CultureInfo.InvariantCulture);
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

                var raw = ReadUnsigned(input, spec.ByteCount, spec.Endian);

                if (!spec.Signed)
                    return raw;

                // sign-extend from N bytes to 64 bit
                var bits = spec.ByteCount * 8;
                if (bits < 64 && (raw & (1UL << (bits - 1))) != 0)
                    raw |= ulong.MaxValue << bits;

                return (long)raw;
            }

            case NumericExtractionType.FLOAT32:
            {
                if (input.Length < 4)
                    return null;

                var f = spec.Endian == Endianness.BIG
                    ? BinaryPrimitives.ReadSingleBigEndian(input)
                    : BinaryPrimitives.ReadSingleLittleEndian(input);

                if (!float.IsFinite(f))
                    return null;

                // via the shortest round-trip string, so 3.3f gives 3.3 instead of 3.299999952316284
                return double.Parse(f.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            }

            case NumericExtractionType.HEX_ASCII:
            {
                if (ascii.Length == 0)
                    return null;
                
                if (ulong.TryParse(ascii, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
                    return hex;

                return null;
            }

            case NumericExtractionType.BIN_ASCII:
            {
                var digits = ascii.Trim();
                if (digits.Length == 0)
                    return null;

                try
                {
                    return Convert.ToUInt64(digits, 2);
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

                if (long.TryParse(ascii, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dec))
                    return dec;

                return null;
            }
            case NumericExtractionType.FLOAT_ASCII:
            {
                if (ascii.Length == 0)
                    return null;

                // accepts ',' as decimal separator, like results without a custom mask
                if (NumberHelper.TryParseDecimal(ascii, out var f))
                    return f;

                return null;
            }
            default:
                throw new NotSupportedException($"Numeric extraction {spec} is not supported.");
        }
    }

    private static readonly Regex FloatToken = new(@"^\s*([+-]?(?:\d+(?:[.,]\d*)?|[.,]\d+)(?:[eE][+-]?\d+)?)", RegexOptions.Compiled);
    private static readonly Regex DecToken = new(@"^\s*([+-]?\d+)", RegexOptions.Compiled);
    private static readonly Regex HexToken = new(@"^\s*(?:0[xX])?([0-9A-Fa-f]+)", RegexOptions.Compiled);
    private static readonly Regex BinToken = new(@"^\s*([01]+)", RegexOptions.Compiled);
    
    private static string ReadNumberToken(string text, NumericExtractionType type)
    {
        var regex = type switch
        {
            NumericExtractionType.FLOAT_ASCII => FloatToken,
            NumericExtractionType.DEC_ASCII => DecToken,
            NumericExtractionType.HEX_ASCII => HexToken,
            NumericExtractionType.BIN_ASCII => BinToken,
            _ => throw new NotSupportedException($"{type} is not a text extraction.")
        };

        var match = regex.Match(text);
        return match.Success ? match.Groups[1].Value : "";
    }

    private static ulong ReadUnsigned(byte[] input, int byteCount, Endianness endian)
    {
        ulong raw = 0;
        for (var b = 0; b < byteCount; b++)
        {
            var index = endian == Endianness.BIG ? b : byteCount - 1 - b;
            raw = (raw << 8) | input[index];
        }

        return raw;
    }
}

#region Mask parsing and rules

internal enum NumericExtractionType
{
    BYTE,
    HEX_ASCII,
    BIN_ASCII,
    DEC_ASCII,
    FLOAT_ASCII,
    FLOAT32
}

internal enum Endianness
{
    BIG,
    LITTLE
}

internal sealed class NumericExtractionSpec
{
    public NumericExtractionType Type { get; }
    public int ByteCount { get; }
    public Endianness Endian { get; }
    public bool Signed { get; }

    private string EndianSuffix => Endian == Endianness.BIG ? "be" : "le";

    public NumericExtractionSpec(NumericExtractionType type, int byteCount = 0, Endianness endian = Endianness.BIG,
        bool signed = false)
    {
        Type = type;
        ByteCount = byteCount;
        Endian = endian;
        Signed = signed;
    }

    public override string ToString() => Type switch
    {
        NumericExtractionType.BYTE => $"{{byte:{ByteCount},{EndianSuffix}{(Signed ? ",signed" : "")}}}",
        NumericExtractionType.FLOAT32 => $"{{f32:{EndianSuffix}}}",
        NumericExtractionType.HEX_ASCII => "{hex}",
        NumericExtractionType.BIN_ASCII => "{bin}",
        NumericExtractionType.DEC_ASCII => "{dec}",
        NumericExtractionType.FLOAT_ASCII => "{float}",
        _ => Type.ToString()
    };
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
    
    public int ExtractionAfterRule { get; set; } = -1;
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
                    result.ExtractionAfterRule = result.MatchRules.Count - 1;
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

        // {name} or {name:option,option,…}
        var colon = content.IndexOf(':');
        var name = (colon < 0 ? content : content[..colon]).Trim().ToLowerInvariant();
        var options = colon < 0
            ? Array.Empty<string>()
            : content[(colon + 1)..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        switch (name)
        {
            case "hex": return new NumericExtractionSpec(NumericExtractionType.HEX_ASCII);
            case "bin": return new NumericExtractionSpec(NumericExtractionType.BIN_ASCII);
            case "dec": return new NumericExtractionSpec(NumericExtractionType.DEC_ASCII);
            case "float": return new NumericExtractionSpec(NumericExtractionType.FLOAT_ASCII);

            case "byte":
            {
                if (options.Length == 0
                    || !int.TryParse(options[0], NumberStyles.None, CultureInfo.InvariantCulture, out var byteCount)
                    || byteCount is < 1 or > 8)
                    throw new FormatException("byte count must be between 1 and 8");

                var (endian, signed) = ParseBinaryOptions(options.Skip(1), allowSigned: true);
                return new NumericExtractionSpec(NumericExtractionType.BYTE, byteCount, endian, signed);
            }

            case "f32":
            {
                var (endian, _) = ParseBinaryOptions(options, allowSigned: false);
                return new NumericExtractionSpec(NumericExtractionType.FLOAT32, 4, endian);
            }

            default:
                throw new FormatException($"unknown numeric extraction '{name}'");
        }
    }

    private static (Endianness Endian, bool Signed) ParseBinaryOptions(IEnumerable<string> options, bool allowSigned)
    {
        var endian = Endianness.BIG;
        var signed = false;

        foreach (var option in options)
        {
            switch (option.ToLowerInvariant())
            {
                case "be":
                    endian = Endianness.BIG;
                    break;
                case "le":
                    endian = Endianness.LITTLE;
                    break;
                case "signed" when allowSigned:
                    signed = true;
                    break;
                default:
                    throw new FormatException($"unknown option '{option}'");
            }
        }

        return (endian, signed);
    }
}

#endregion