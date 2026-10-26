using System;
using System.Globalization;

namespace ATLab.Helpers;

/// <summary>
/// Number parsing that accepts ',' and '.' as decimal separator, and rounding used when comparing values with
/// limits.
/// </summary>
public static class NumberHelper
{
    public static double RoundToSignificantDigits(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return value;

        return double.Parse(value.ToString("G15", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }
    
    public static bool TryParseDecimal(string? input, out double result) =>
        double.TryParse(input?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    /// <summary>7 bit I²C address 0-127, decimal or hex with 0x prefix.</summary>
    public static bool TryParseI2CAddress(string? input, out int address)
    {
        var text = input?.Trim() ?? string.Empty;
        var isValid = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? int.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address)
            : int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out address);

        return isValid && address is >= 0 and <= 0x7F;
    }
}