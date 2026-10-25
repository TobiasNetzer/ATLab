using System;
using System.Collections;
using ATLab.ViewModels;

namespace ATLab.Helpers;

/// <summary>
/// Sorts pin table rows in natural order, so pin "2" comes before "10" and "IO(2)" before "IO(10)".
/// </summary>
public class BoundaryScanPinComparer : IComparer
{
    public static BoundaryScanPinComparer ByPin { get; } = new(p => p.PinName);
    public static BoundaryScanPinComparer ByPort { get; } = new(p => p.Port);
    public static BoundaryScanPinComparer ByLabel { get; } = new(p => p.Label);

    private readonly Func<BoundaryScanPinViewModel, string> _selector;

    private BoundaryScanPinComparer(Func<BoundaryScanPinViewModel, string> selector)
    {
        _selector = selector;
    }

    public int Compare(object? x, object? y)
    {
        return NaturalStringComparer.Instance.Compare(
            x is BoundaryScanPinViewModel pinX ? _selector(pinX) : null,
            y is BoundaryScanPinViewModel pinY ? _selector(pinY) : null);
    }
}