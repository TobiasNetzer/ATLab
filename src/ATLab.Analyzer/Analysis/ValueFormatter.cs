using System;
using System.Globalization;

namespace ATLab.Analyzer.Analysis;

/// <summary>
/// Formats values like ATLab: with an SI prefix (0.0047 "V" → "4.7mV"), or unscaled when the unit is in braces
/// ("{°C}" → "23.5°C").
/// </summary>
public static class ValueFormatter
{
    private static readonly (double Factor, string Prefix)[] Prefixes =
    [
        (1e9, "G"), (1e6, "M"), (1e3, "k"), (1, ""), (1e-3, "m"), (1e-6, "u"), (1e-9, "n"), (1e-12, "p"), (1e-15, "f")
    ];

    /// <param name="value">The value without prefix.</param>
    /// <param name="unit">The unit as in the result file, may be empty or in braces.</param>
    /// <param name="significantDigits">Digits shown, trailing zeros are dropped (111.7057 mA → 111.71mA).</param>
    /// <param name="culture">Number format, the current culture if null.</param>
    public static string Format(double? value, string? unit, int significantDigits = 5, CultureInfo? culture = null)
    {
        if (value is not { } v || !double.IsFinite(v))
            return string.Empty;

        culture ??= CultureInfo.CurrentCulture;
        unit ??= string.Empty;

        if (unit.Length == 0 || IsUnscaled(unit))
            return Round(v, significantDigits, culture) + DisplayUnit(unit);

        var abs = Math.Abs(v);
        var (factor, prefix) = (1d, "");

        if (abs > 0)
        {
            foreach (var candidate in Prefixes)
            {
                (factor, prefix) = candidate;
                if (abs >= candidate.Factor)
                    break;
            }
        }

        return Round(v / factor, significantDigits, culture) + prefix + unit;
    }

    /// <summary>
    /// Reads a value typed by the user, with an optional SI prefix and unit: "5m", "5 mV", "0,005", "2.5k".
    /// Both decimal separators are accepted.
    /// </summary>
    public static bool TryParse(string? text, string? unit, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var s = text.Trim().Replace(" ", "");
        var unitText = DisplayUnit(unit);
        if (unitText.Length > 0 && s.EndsWith(unitText, StringComparison.OrdinalIgnoreCase))
            s = s[..^unitText.Length];

        var factor = 1d;
        if (s.Length > 1 && !char.IsDigit(s[^1]))
        {
            factor = s[^1] switch
            {
                'G' => 1e9, 'M' => 1e6, 'k' or 'K' => 1e3, 'm' => 1e-3, 'u' or 'µ' or 'μ' => 1e-6, 'n' => 1e-9, 'p' => 1e-12,
                _ => double.NaN
            };
            s = s[..^1];
        }

        if (double.IsNaN(factor) ||
            !double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            return false;

        value = number * factor;
        return double.IsFinite(value);
    }

    /// <summary>True for a unit in braces, which ATLab shows without SI prefix.</summary>
    public static bool IsUnscaled(string? unit) =>
        unit is { Length: >= 2 } && unit[0] == '{' && unit[^1] == '}';

    /// <summary>The unit text without braces, for tables and exports.</summary>
    public static string DisplayUnit(string? unit) =>
        IsUnscaled(unit) ? unit![1..^1] : unit ?? string.Empty;

    private static string Round(double value, int significantDigits, CultureInfo culture)
    {
        // Digits before the decimal point; negative for leading zeros after it (0.0012 → -2)
        var integerDigits = value == 0 ? 1 : (int)Math.Floor(Math.Log10(Math.Abs(value))) + 1;
        var decimals = Math.Clamp(significantDigits - integerDigits, 0, 12);
        return value.ToString("0." + new string('#', decimals), culture);
    }
}