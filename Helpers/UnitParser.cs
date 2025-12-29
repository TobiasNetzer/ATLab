using System;
using System.Globalization;

namespace ATLab.Helpers;

public static class UnitParser
{
    public static bool TryParse(string? input, out double result, string? unit = null)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(input)) return false;

        input = input.Trim();
        
        if (!string.IsNullOrEmpty(unit) && input.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
        {
            input = input.Substring(0, input.Length - unit.Length).Trim();
        }

        double multiplier = 1.0;
        
        if (input.EndsWith("p"))
        {
            multiplier = 1e-12;
            input = input.Substring(0, input.Length - 1);
        }
        else if (input.EndsWith("n"))
        {
            multiplier = 1e-9;
            input = input.Substring(0, input.Length - 1);
        }
        else if (input.EndsWith("u"))
        {
            multiplier = 1e-6;
            input = input.Substring(0, input.Length - 1);
        }
        else if (input.EndsWith("m"))
        {
            multiplier = 1e-3;
            input = input.Substring(0, input.Length - 1);
        }
        else if (input.EndsWith("k"))
        {
            multiplier = 1e3;
            input = input.Substring(0, input.Length - 1);
        }
        else if (input.EndsWith("M"))
        {
            multiplier = 1e6;
            input = input.Substring(0, input.Length - 1);
        }

        if (double.TryParse(input, CultureInfo.CurrentCulture, out double value))
        {
            result = Math.Round(value * multiplier, 15);
            return true;
        }

        return false;
    }

    public static string Format(double value, string? unit = null)
    {
        double absValue = Math.Abs(value);
        string suffix = "";
        double displayValue = value;

        if (absValue >= 1e6)
        {
            displayValue = value / 1e6;
            suffix = "M";
        }
        else if (absValue >= 1e3)
        {
            displayValue = value / 1e3;
            suffix = "k";
        }
        else if (absValue >= 1 || absValue == 0)
        {
            displayValue = value;
            suffix = "";
        }
        else if (absValue >= 1e-3)
        {
            displayValue = value / 1e-3;
            suffix = "m";
        }
        else if (absValue >= 1e-6)
        {
            displayValue = value / 1e-6;
            suffix = "u";
        }
        else if (absValue >= 1e-9)
        {
            displayValue = value / 1e-9;
            suffix = "n";
        }
        else if (absValue >= 1e-12)
        {
            displayValue = value / 1e-12;
            suffix = "p";
        }

        return $"{Math.Round(displayValue, 3).ToString("0.###", CultureInfo.CurrentCulture)}{suffix}{unit ?? ""}";
    }
}
