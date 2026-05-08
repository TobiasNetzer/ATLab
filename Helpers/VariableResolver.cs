using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
        string expression,
        IEnumerable<CustomVariable>? runtimeVariables)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return string.Empty;
        
        var substituted = _regex.Replace(expression, match =>
        {
            var name = match.Groups[1].Value;
            var format = match.Groups[2].Success ? match.Groups[2].Value : null;
            var defaultValue = match.Groups[3].Success ? match.Groups[3].Value : null;

            var variable = runtimeVariables?.FirstOrDefault(v => v.Name == name);

            var value = variable?.Value ?? defaultValue;

            if (value == null)
                return "0.0"; // undefined → treat as zero for math

            return ApplyFormat(value, format);
        });

        // If it looks like a math expression, we need to normalize separators to dot for DataTable.Compute
        var mathReady = substituted.Replace(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, ".");
        
        try
        {
            var normalizedExpr = EnsureDoublePrecision(mathReady);
            var result = EvaluateMath(normalizedExpr);
            return result.ToString(CultureInfo.CurrentCulture);
        }
        catch
        {
            return substituted;
        }
    }

    private static string EnsureDoublePrecision(string expr)
    {
        // DataTable.Compute always expects '.' as decimal separator
        return Regex.Replace(expr, @"(?<![\d\.])(\d+)(?![\d\.])", "$1.0");
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
    
    private static double EvaluateMath(string expr)
    {
        var dt = new DataTable();
        dt.Locale = CultureInfo.InvariantCulture;

        var result = dt.Compute(expr, string.Empty);
        return Convert.ToDouble(result, CultureInfo.InvariantCulture);
    }
}