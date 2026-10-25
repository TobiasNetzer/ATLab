using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ATLab.Enums;
using ATLab.Helpers;
using ATLab.Services;
using ATLab.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;

namespace ATLab.Controls;

/// <summary>
/// Draws the device package with the live state of every pin. The layout is derived from the pin names:
/// letter + number names form a BGA grid, numbered pins are placed around a quad or dual-in-line body.
/// </summary>
public partial class BoundaryScanPackageView : Control
{
    public static readonly StyledProperty<IEnumerable?> PinsProperty =
        AvaloniaProperty.Register<BoundaryScanPackageView, IEnumerable?>(nameof(Pins));

    public static readonly StyledProperty<string?> PackageNameProperty =
        AvaloniaProperty.Register<BoundaryScanPackageView, string?>(nameof(PackageName));

    public static readonly StyledProperty<int> StateVersionProperty =
        AvaloniaProperty.Register<BoundaryScanPackageView, int>(nameof(StateVersion));

    public static readonly StyledProperty<BoundaryScanPinViewModel?> SelectedPinProperty =
        AvaloniaProperty.Register<BoundaryScanPackageView, BoundaryScanPinViewModel?>(
            nameof(SelectedPin), defaultBindingMode: BindingMode.TwoWay);

    private static readonly Color HighColor = Color.Parse("#F59E0B");
    private static readonly Color LowColor = Color.Parse("#3B82F6");
    private static readonly Color TapColor = Color.Parse("#A78BFA");

    private const double InfoHeight = 26;

    private readonly List<(BoundaryScanPinViewModel? Pin, string Name, Rect Bounds, bool Round)> _layout = new();
    private BoundaryScanPinViewModel? _hoveredPin;
    private INotifyCollectionChanged? _observedPins;

    static BoundaryScanPackageView()
    {
        AffectsRender<BoundaryScanPackageView>(PinsProperty, PackageNameProperty, StateVersionProperty, SelectedPinProperty);
    }

    public BoundaryScanPackageView()
    {
        ClipToBounds = true;
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
    }

    public IEnumerable? Pins
    {
        get => GetValue(PinsProperty);
        set => SetValue(PinsProperty, value);
    }

    public string? PackageName
    {
        get => GetValue(PackageNameProperty);
        set => SetValue(PackageNameProperty, value);
    }

    public int StateVersion
    {
        get => GetValue(StateVersionProperty);
        set => SetValue(StateVersionProperty, value);
    }

    public BoundaryScanPinViewModel? SelectedPin
    {
        get => GetValue(SelectedPinProperty);
        set => SetValue(SelectedPinProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PinsProperty)
        {
            if (_observedPins != null)
                _observedPins.CollectionChanged -= PinsCollectionChanged;

            _observedPins = change.NewValue as INotifyCollectionChanged;

            if (_observedPins != null)
                _observedPins.CollectionChanged += PinsCollectionChanged;
        }
    }

    private void PinsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var foreground = GetBrush("ForegroundColor", Colors.White);
        var muted = GetBrush("MutedColor", Colors.Gray);
        var outline = GetBrush("OutlineColor", Colors.DimGray);
        var info = GetBrush("InfoColor", Colors.DodgerBlue);

        var pins = Pins?.OfType<BoundaryScanPinViewModel>().ToList() ?? [];
        var area = new Rect(0, 0, Bounds.Width, Math.Max(0, Bounds.Height - InfoHeight)).Deflate(12);

        _layout.Clear();

        if (pins.Count == 0 || area.Width < 40 || area.Height < 40)
        {
            DrawText(context, LocalizationService.Instance["Text_NoPackageInformation"], 14, muted, Bounds.Center, centered: true);
            return;
        }

        var body = BuildLayout(pins, area);

        if (body is { } bodyRect)
        {
            context.DrawRectangle(outline, new Pen(muted, 1), bodyRect, 6, 6);

            var marker = Math.Clamp(Math.Min(bodyRect.Width, bodyRect.Height) * 0.05, 4, 10);
            context.DrawEllipse(muted, null, new Point(bodyRect.X + marker * 2, bodyRect.Y + marker * 2), marker, marker);

            DrawText(context, PackageName ?? string.Empty, Math.Clamp(bodyRect.Width / 14, 10, 20), foreground,
                bodyRect.Center, centered: true);
        }

        foreach (var (pin, name, bounds, round) in _layout)
        {
            var fill = pin == null ? null : PinBrush(pin, muted);
            var isDriven = pin?.DriveState is PinDriveState.LOW or PinDriveState.HIGH && pin.IsDriveEnabled;

            // Hover stays at least as thick as the driven outline so it does not disappear
            var pen = pin == _hoveredPin && pin != null ? new Pen(foreground, isDriven ? 3.5 : 2)
                : isDriven ? new Pen(foreground, 3)
                : new Pen(muted, pin == null ? 0.5 : 1);

            DrawPinShape(context, fill, pen, bounds, round);

            // The selection is a ring outside the pin, so it shows on every fill and keeps the driven outline visible
            if (pin != null && pin == SelectedPin)
                DrawPinShape(context, null, new Pen(info, 2.5), bounds.Inflate(4), round);

            if (body == null || Math.Min(bounds.Width, bounds.Height) >= 18)
                DrawText(context, name, Math.Min(bounds.Width, bounds.Height) * 0.4, foreground, bounds.Center, centered: true);
        }

        var described = _hoveredPin ?? SelectedPin;
        if (described != null)
            DrawText(context, Describe(described), 13, foreground, new Point(12, Bounds.Height - InfoHeight + 4), centered: false);
    }

    private static void DrawPinShape(DrawingContext context, IBrush? fill, IPen pen, Rect bounds, bool round)
    {
        if (round)
            context.DrawEllipse(fill, pen, bounds.Center, bounds.Width / 2, bounds.Height / 2);
        else
            context.DrawRectangle(fill, pen, bounds, 2, 2);
    }

    private IBrush? PinBrush(BoundaryScanPinViewModel pin, IBrush muted)
    {
        if (pin.IsTapPin)
            return new SolidColorBrush(TapColor);

        if (pin.InputValue is { } level)
            return new SolidColorBrush(level ? HighColor : LowColor);

        if (pin.IsDriveEnabled && pin.DriveState is PinDriveState.LOW or PinDriveState.HIGH)
            return new SolidColorBrush(pin.DriveState == PinDriveState.HIGH ? HighColor : LowColor);

        return pin.IsLinkage ? muted : null;
    }

    private static string Describe(BoundaryScanPinViewModel pin)
    {
        var localization = LocalizationService.Instance;
        var text = $"{localization["Header_Pin"]} {pin.PinName}  ·  {pin.Port}";

        if (!string.IsNullOrWhiteSpace(pin.Label))
            text += $"  ·  {pin.Label}";

        text += $"  ·  {pin.DirectionText}";

        if (pin.InputValue is { } level)
            text += $"  ·  {localization["Header_Input"]} {(level ? 1 : 0)}";

        if (pin.DriveState is { } drive)
            text += $"  ·  {localization["Header_Output"]} {drive switch { PinDriveState.HIGH => "1", PinDriveState.LOW => "0", _ => "Z" }}";

        if (pin.CellText.Length > 0)
            text += $"  ·  {localization["Text_Cells"]} {pin.CellText}";

        return text;
    }

    /// <summary>Fills the layout list and returns the package body, or null for a plain pin grid.</summary>
    private Rect? BuildLayout(List<BoundaryScanPinViewModel> pins, Rect area)
    {
        var byName = new Dictionary<string, BoundaryScanPinViewModel>(StringComparer.OrdinalIgnoreCase);
        foreach (var pin in pins)
            byName.TryAdd(pin.PinName, pin);

        if (byName.Keys.All(n => int.TryParse(n, out _)))
        {
            var count = byName.Keys.Max(n => int.Parse(n, CultureInfo.InvariantCulture));
            return IsDualPackage(count) ? LayoutDual(byName, count, area) : LayoutQuad(byName, count, area);
        }

        if (byName.Keys.All(n => GridPinRegex().IsMatch(n)))
            return LayoutGrid(byName, area);

        LayoutList(byName, area);
        return null;
    }

    private bool IsDualPackage(int pinCount)
    {
        var name = (PackageName ?? string.Empty).ToUpperInvariant();

        if (new[] { "QFP", "QFN", "LCC", "QUAD" }.Any(name.Contains))
            return false;

        if (new[] { "SO", "DIP", "SOP", "DFN", "SON" }.Any(name.Contains))
            return true;

        return pinCount < 20 || pinCount % 4 != 0;
    }

    // Pin 1 at the top of the left side, numbered counterclockwise
    private Rect LayoutQuad(Dictionary<string, BoundaryScanPinViewModel> pins, int count, Rect area)
    {
        var perSide = (int)Math.Ceiling(count / 4.0);
        var size = Math.Min(area.Width, area.Height);
        var pitch = size / (perSide + 1 + 2 * 2.5);
        var length = pitch * 2.5;
        var body = new Rect(area.Center.X - size / 2 + length, area.Center.Y - size / 2 + length, size - 2 * length, size - 2 * length);
        var thickness = pitch * 0.7;

        for (var number = 1; number <= count; number++)
        {
            var side = (number - 1) / perSide;
            var index = (number - 1) % perSide;
            var offset = (index + 1) * pitch - thickness / 2;

            var bounds = side switch
            {
                0 => new Rect(body.Left - length, body.Top + offset, length, thickness),
                1 => new Rect(body.Left + offset, body.Bottom, thickness, length),
                2 => new Rect(body.Right, body.Bottom - offset - thickness, length, thickness),
                _ => new Rect(body.Right - offset - thickness, body.Top - length, thickness, length)
            };

            AddPin(pins, number.ToString(CultureInfo.InvariantCulture), bounds, round: false);
        }

        return body;
    }

    // Pin 1 at the top of the left side, the right side counts upwards
    private Rect LayoutDual(Dictionary<string, BoundaryScanPinViewModel> pins, int count, Rect area)
    {
        var perSide = (int)Math.Ceiling(count / 2.0);
        var pitch = Math.Min(area.Height / (perSide + 1), 40);
        var length = Math.Max(pitch * 1.5, 12);
        var height = pitch * (perSide + 1);
        var width = Math.Max(Math.Min(height * 0.45, area.Width - 2 * length), 40);
        var body = new Rect(area.Center.X - width / 2, area.Center.Y - height / 2, width, height);
        var thickness = pitch * 0.7;

        for (var number = 1; number <= count; number++)
        {
            var left = number <= perSide;
            var index = left ? number - 1 : count - number;
            var top = body.Top + (index + 1) * pitch - thickness / 2;

            var bounds = left
                ? new Rect(body.Left - length, top, length, thickness)
                : new Rect(body.Right, top, length, thickness);

            AddPin(pins, number.ToString(CultureInfo.InvariantCulture), bounds, round: false);
        }

        return body;
    }

    private Rect LayoutGrid(Dictionary<string, BoundaryScanPinViewModel> pins, Rect area)
    {
        var parsed = pins.Keys
            .Select(n => GridPinRegex().Match(n))
            .Select(m => (Row: m.Groups[1].Value.ToUpperInvariant(), Column: int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)))
            .ToList();

        var rows = parsed.Select(p => p.Row).Distinct()
            .OrderBy(r => r.Length).ThenBy(r => r, StringComparer.Ordinal)
            .ToList();
        var columns = parsed.Max(p => p.Column);

        var pitch = Math.Min(area.Width / (columns + 1), area.Height / (rows.Count + 1));
        var body = new Rect(
            area.Center.X - pitch * (columns + 1) / 2,
            area.Center.Y - pitch * (rows.Count + 1) / 2,
            pitch * (columns + 1),
            pitch * (rows.Count + 1));

        var diameter = pitch * 0.75;

        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 1; c <= columns; c++)
            {
                var name = $"{rows[r]}{c}";
                var center = new Point(body.Left + c * pitch, body.Top + (r + 1) * pitch);
                var bounds = new Rect(center.X - diameter / 2, center.Y - diameter / 2, diameter, diameter);

                // Positions without a pin (depopulated balls) are left out
                if (pins.ContainsKey(name))
                    AddPin(pins, name, bounds, round: true);
            }
        }

        return body;
    }

    private void LayoutList(Dictionary<string, BoundaryScanPinViewModel> pins, Rect area)
    {
        var names = pins.Keys.OrderBy(n => n, NaturalStringComparer.Instance).ToList();
        var columns = (int)Math.Ceiling(Math.Sqrt(names.Count * area.Width / area.Height));
        var rows = (int)Math.Ceiling(names.Count / (double)columns);
        var pitch = Math.Min(area.Width / columns, area.Height / rows);

        for (var i = 0; i < names.Count; i++)
        {
            var bounds = new Rect(area.Left + i % columns * pitch, area.Top + i / columns * pitch, pitch, pitch).Deflate(pitch * 0.1);
            AddPin(pins, names[i], bounds, round: false);
        }
    }

    private void AddPin(Dictionary<string, BoundaryScanPinViewModel> pins, string name, Rect bounds, bool round)
    {
        _layout.Add((pins.GetValueOrDefault(name), name, bounds, round));
    }

    private BoundaryScanPinViewModel? HitTest(Point point)
    {
        foreach (var (pin, _, bounds, _) in _layout)
        {
            if (pin != null && bounds.Inflate(1).Contains(point))
                return pin;
        }

        return null;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var pin = HitTest(e.GetPosition(this));
        if (pin == _hoveredPin)
            return;

        _hoveredPin = pin;
        Cursor = pin != null ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hoveredPin = null;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var pin = HitTest(e.GetPosition(this));
        if (pin == null)
            return;

        SelectedPin = pin;

        // Double click toggles a driven output
        if (e.ClickCount == 2)
            pin.Toggle();

        e.Handled = true;
    }

    private IBrush GetBrush(string key, Color fallback)
    {
        if (this.TryFindResource(key, ActualThemeVariant, out var resource))
        {
            if (resource is IBrush brush)
                return brush;

            if (resource is Color color)
                return new SolidColorBrush(color);
        }

        return new SolidColorBrush(fallback);
    }

    private static void DrawText(DrawingContext context, string text, double size, IBrush brush, Point origin, bool centered)
    {
        if (string.IsNullOrEmpty(text) || size < 6)
            return;

        var formatted = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            Typeface.Default, size, brush);

        var position = centered
            ? new Point(origin.X - formatted.Width / 2, origin.Y - formatted.Height / 2)
            : origin;

        context.DrawText(formatted, position);
    }

    [GeneratedRegex(@"^([A-Za-z]{1,3})(\d{1,3})$")]
    private static partial Regex GridPinRegex();
}