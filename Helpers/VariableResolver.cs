using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ATLab.Models;

namespace ATLab.Helpers;

public static class VariableResolver
{
    private static readonly Regex _regex = new(
        @"\{([A-Za-z0-9_]+)(?:\|([A-Za-z0-9_]+)(?:=([^}]+))?)?\}",
        RegexOptions.Compiled
    );

    public static string Resolve(
        string commandText,
        IEnumerable<CustomVariable>? runtimeVariables)
    {
        return _regex.Replace(commandText, match =>
        {
            var name = match.Groups[1].Value;
            var format = match.Groups[2].Success ? match.Groups[2].Value : null;
            var defaultValue = match.Groups[3].Success ? match.Groups[3].Value : null;

            var variable = runtimeVariables?.FirstOrDefault(v => v.Name == name);

            var value = variable?.Value ?? defaultValue;
            return value == null
                ? match.Value
                : ApplyFormat(value, format);
        });
    }

    private static string ApplyFormat(string value, string? format)
    {
        if (format == null)
            return value;

        return format.ToLowerInvariant() switch
        {
            "hex"    => ConvertToHex(value),
            "bin"    => ConvertToBinary(value),
            "ascii"  => ConvertToAscii(value),
            "bytes"  => ConvertToByteString(value),
            "upper"  => value.ToUpperInvariant(),
            "lower"  => value.ToLowerInvariant(),
            "len"    => value.Length.ToString(),
            "base64" => Convert.ToBase64String(Encoding.UTF8.GetBytes(value)),
            _        => value
        };
    }

    private static string ConvertToHex(string value) =>
        BitConverter.ToString(Encoding.UTF8.GetBytes(value)).Replace("-", "");

    private static string ConvertToBinary(string value) =>
        string.Join("", Encoding.UTF8.GetBytes(value)
            .Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

    private static string ConvertToAscii(string value) =>
        string.Join(" ", value.Select(c => ((int)c).ToString()));

    private static string ConvertToByteString(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return "[" + string.Join(", ", bytes) + "]";
    }
}