using System;
using System.Globalization;

namespace ATLab.Helpers;

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
}