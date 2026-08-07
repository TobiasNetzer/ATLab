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

        var multiplier = 1.0;
        
        if (input.EndsWith('p'))
        {
            multiplier = 1e-12;
            input = input.Substring(0, input.Length - 1);
        }
        else if (input.EndsWith('n'))
        {
            multiplier = 1e-9;
            input = input.Substring(0, input.Length - 1);
        }
        else if (input.EndsWith('u'))
        {
            multiplier = 1e-6;
            input = input.Substring(0, input.Length - 1);
        }
        else if (input.EndsWith('m'))
        {
            multiplier = 1e-3;
            input = input.Substring(0, input.Length - 1);
        }
        else if (input.EndsWith('k'))
        {
            multiplier = 1e3;
            input = input.Substring(0, input.Length - 1);
        }
        else if (input.EndsWith('M'))
        {
            multiplier = 1e6;
            input = input.Substring(0, input.Length - 1);
        }

        input = input.Replace(',', '.');

        if (!double.TryParse(
                input,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var value)) return false;
        result = Math.Round(value * multiplier, 13);
        return true;

    }

    public static string Format(double value, string? unit = null, int precision = 6, bool useInvariantCulture = false)
    {
        var culture = useInvariantCulture ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;

        if (unit != null && unit.StartsWith('{') && unit.EndsWith('}'))
        {
            var trimmedUnit = unit.Trim('{', '}');
            var valueString = value.ToString(culture);
            return $"{valueString}{trimmedUnit}";
        }
        
        var absValue = Math.Abs(value);
        var suffix = "";
        var displayValue = value;

        switch (absValue)
        {
            case >= 1e6:
                displayValue = value / 1e6;
                suffix = "M";
                break;
            case >= 1e3:
                displayValue = value / 1e3;
                suffix = "k";
                break;
            case >= 1:
            case 0:
                displayValue = value;
                suffix = "";
                break;
            case >= 1e-3:
                displayValue = value / 1e-3;
                suffix = "m";
                break;
            case >= 1e-6:
                displayValue = value / 1e-6;
                suffix = "u";
                break;
            case >= 1e-9:
                displayValue = value / 1e-9;
                suffix = "n";
                break;
            case >= 1e-12:
                displayValue = value / 1e-12;
                suffix = "p";
                break;
        }

        var format = "0." + new string('#', precision);
        var formattedValue = displayValue.ToString(format, culture);
        return $"{formattedValue}{suffix}{unit ?? ""}";
    }
}