using System;
using System.Collections.Generic;

namespace ATLab.Helpers;

/// <summary>Sorts "A2" before "A10" and "9" before "10".</summary>
public class NaturalStringComparer : IComparer<string>
{
    public static NaturalStringComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (x == null || y == null)
            return string.Compare(x, y, StringComparison.Ordinal);

        int i = 0, j = 0;

        while (i < x.Length && j < y.Length)
        {
            if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
            {
                var startX = i;
                var startY = j;
                while (i < x.Length && char.IsDigit(x[i])) i++;
                while (j < y.Length && char.IsDigit(y[j])) j++;

                var numberX = x[startX..i].TrimStart('0');
                var numberY = y[startY..j].TrimStart('0');

                var result = numberX.Length != numberY.Length
                    ? numberX.Length.CompareTo(numberY.Length)
                    : string.CompareOrdinal(numberX, numberY);

                if (result != 0)
                    return result;
            }
            else
            {
                var result = char.ToUpperInvariant(x[i]).CompareTo(char.ToUpperInvariant(y[j]));
                if (result != 0)
                    return result;

                i++;
                j++;
            }
        }

        return (x.Length - i).CompareTo(y.Length - j);
    }
}