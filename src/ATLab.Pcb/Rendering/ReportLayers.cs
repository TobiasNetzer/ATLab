using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ATLab.Pcb.Controls;
using ATLab.Pcb.Kicad;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.Pcb.Rendering;

/// <summary>
/// The user's choice of board layers for printed pictures (ATLab's test report, the Designer's build sheet): which
/// layers each kind of picture draws (a view, e.g. "top" or "assemblyBottom"), and for all of them their order, color
/// and opacity. Only what differs from the defaults is stored; null means the default.
/// </summary>
public sealed class ReportLayerSettings
{
    /// <summary>
    /// Layers each view draws, by the key of the view ("@PadsTop", "@PadsBottom" and "@Holes" for pads and holes); a
    /// view without an entry draws its defaults.
    /// </summary>
    public Dictionary<string, List<string>>? Views { get; set; }

    /// <summary>Opacity in percent of the layers the user changed.</summary>
    public Dictionary<string, int>? Opacity { get; set; }

    /// <summary>Order of the layers, the topmost first; null for the automatic order.</summary>
    public List<string>? Order { get; set; }

    /// <summary>Colors ("#RRGGBB") of the layers the user changed.</summary>
    public Dictionary<string, string>? Colors { get; set; }

    public ReportLayerSettings Copy() => new()
    {
        Views = Views?.ToDictionary(p => p.Key, p => p.Value.ToList()),
        Opacity = Opacity is null ? null : new(Opacity),
        Order = Order is null ? null : [.. Order],
        Colors = Colors is null ? null : new(Colors)
    };

    /// <summary>Layers of a view, or null without a choice.</summary>
    public List<string>? LayersOf(string view) => Views is not null && Views.TryGetValue(view, out var layers) ? layers : null;

    public void SetLayers(string view, List<string> layers)
    {
        Views ??= [];
        Views[view] = layers;
    }

    /// <summary>These settings, with the fallback's values where these have none (e.g. the user's default).</summary>
    public ReportLayerSettings Over(ReportLayerSettings? fallback)
    {
        var copy = Copy();
        if (fallback is null)
            return copy;

        var other = fallback.Copy();
        foreach (var (view, layers) in other.Views ?? [])
        {
            copy.Views ??= [];
            copy.Views.TryAdd(view, layers);
        }

        copy.Opacity ??= other.Opacity;
        copy.Order ??= other.Order;
        copy.Colors ??= other.Colors;
        return copy;
    }

    /// <summary>What the renderer needs for one view; the given layers when the view has no choice yet.</summary>
    public BoardLayerStyle StyleFor(string view, IReadOnlyCollection<string> defaultLayers) => new(
        LayersOf(view) ?? defaultLayers,
        Opacity?.ToDictionary(p => p.Key, p => p.Value / 100.0),
        Order,
        Colors?.Where(p => Color.TryParse(p.Value, out _)).ToDictionary(p => p.Key, p => Color.Parse(p.Value)));
}

/// <summary>Layers, opacity (0 … 1), order (topmost first) and colors for drawing one side of a board.</summary>
public sealed record BoardLayerStyle(
    IReadOnlyCollection<string> Layers,
    IReadOnlyDictionary<string, double>? Opacity = null,
    IReadOnlyList<string>? Stack = null,
    IReadOnlyDictionary<string, Color>? Colors = null);

/// <summary>
/// A kind of picture with layers of its own, a column of the layer list: its key in the settings, its short name for
/// the column and its full name, both in the current language, and the layers it draws without a choice.
/// </summary>
public partial class ReportLayerView(string key, Func<string> label, IReadOnlyCollection<string> defaults, Func<string>? toolTip = null)
    : ObservableObject
{
    public string Key { get; } = key;

    public IReadOnlyCollection<string> Defaults { get; } = defaults;

    public string Label => label();

    public string ToolTip => toolTip?.Invoke() ?? label();

    public void RefreshLabel()
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(ToolTip));
    }
}

/// <summary>Width of a view's column in the layer list, the same for its heading and its check boxes.</summary>
public static class ReportLayerColumns
{
    public const double Width = 80;
}

/// <summary>Whether a layer is drawn in one view: a check box of the layer list.</summary>
public partial class ReportLayerToggle(ReportLayerView view) : ObservableObject
{
    public ReportLayerView View { get; } = view;

    [ObservableProperty] private bool _isVisible;
}

/// <summary>
/// A layer in the layer list of the printed pictures: drawn in which views, its color (black by default) and its
/// opacity in percent; the swatch shows both.
/// </summary>
public partial class ReportLayerOption : ObservableObject
{
    public ReportLayerOption(string name, string display, IEnumerable<ReportLayerView> views)
    {
        Name = name;
        Display = display;
        Toggles = views.Select(v => new ReportLayerToggle(v)).ToList();
        foreach (var toggle in Toggles)
        {
            toggle.PropertyChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(IsUsed));
                ToggleChanged?.Invoke(toggle);
            };
        }
    }

    public string Name { get; }

    public string Display { get; private set; }

    /// <summary>One check box per view.</summary>
    public IReadOnlyList<ReportLayerToggle> Toggles { get; }

    /// <summary>After a check box of this layer changed.</summary>
    public event Action<ReportLayerToggle>? ToggleChanged;

    /// <summary>Drawn in any view, so its opacity matters.</summary>
    public bool IsUsed => Toggles.Any(t => t.IsVisible);

    public bool IsVisibleIn(string view) => Toggles.FirstOrDefault(t => t.View.Key == view)?.IsVisible == true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Brush), nameof(OpacityText))]
    private int _opacity = 100;

    /// <summary>Color of the layer; opaque, the opacity is set on its own.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Brush))]
    private Color _color = Colors.Black;

    partial void OnColorChanged(Color value)
    {
        if (value.A != 255)
            Color = Color.FromRgb(value.R, value.G, value.B);
    }

    partial void OnOpacityChanged(int value)
    {
        var clamped = Math.Clamp(value, 0, 100);
        if (clamped != value)
            Opacity = clamped;
    }

    public IBrush Brush => new SolidColorBrush(Color, Opacity / 100.0);

    public string OpacityText => $"{Opacity} %";

    public static Color DefaultColor(string layer) => BoardSvgRenderer.DefaultLayerColor(layer);

    public static int DefaultPercent(string layer) => (int)Math.Round(BoardSvgRenderer.DefaultLayerOpacity(layer) * 100);

    public void SetDisplay(string display)
    {
        Display = display;
        OnPropertyChanged(nameof(Display));
    }
}

/// <summary>
/// The layer list of the printed pictures for one board, shared by ATLab and the Designer: a column per view, built
/// from the settings, and every change of the user written back into them, after which <see cref="Changed"/> tells the
/// owner to store them. Layers this board does not have keep their settings for another board.
/// </summary>
public partial class ReportLayerList : ObservableObject
{
    private readonly Func<string, string> _display;
    private ReportLayerSettings _settings = new();
    private bool _isBuilding;
    private Board? _board;
    private BoardSide _orderSide;

    /// <param name="display">Name of a layer in the list, e.g. the translated name of the pads.</param>
    /// <param name="views">The kinds of pictures, one column each.</param>
    public ReportLayerList(Func<string, string> display, IReadOnlyList<ReportLayerView> views)
    {
        _display = display;
        Views = views;
    }

    public IReadOnlyList<ReportLayerView> Views { get; }

    /// <summary>Pads, holes and the KiCad layers of the board, the topmost first.</summary>
    public ObservableCollection<ReportLayerOption> Options { get; } = [];

    public bool HasOptions => Options.Count > 0;

    public bool HasCustomOrder => _settings.Order is { Count: > 0 };

    /// <summary>The settings as the user left them; the owner stores them.</summary>
    public ReportLayerSettings Settings => _settings;

    /// <summary>
    /// The user's own default from the app settings: used for what the settings leave open, and what "Default style"
    /// and "Default order" go back to; null for the built-in defaults.
    /// </summary>
    public ReportLayerSettings? UserDefault
    {
        get => _userDefault;
        set
        {
            _userDefault = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUserDefault));
            LoadDefaultCommand.NotifyCanExecuteChanged();
        }
    }

    private ReportLayerSettings? _userDefault;

    public bool HasUserDefault => _userDefault is not null;

    /// <summary>The settings as drawn: the user's choice, the user's default for the rest.</summary>
    public ReportLayerSettings Effective => _settings.Over(UserDefault);

    /// <summary>After every change of the user.</summary>
    public event Action? Changed;

    /// <summary>After "Save as default", with the new default; the owner stores it in the app settings.</summary>
    public event Action<ReportLayerSettings>? DefaultSaved;

    /// <summary>The layers as they are now become the default for new projects and those without a choice.</summary>
    [RelayCommand]
    private void SaveAsDefault()
    {
        var current = Effective;
        foreach (var view in Views)
            current.SetLayers(view.Key, Options.Where(o => o.IsVisibleIn(view.Key)).Select(o => o.Name).Order(StringComparer.Ordinal).ToList());
        UserDefault = current;
        DefaultSaved?.Invoke(current.Copy());
    }

    /// <summary>
    /// The saved default as the choice of this project or fixture: copied, so it stays as it is if the default changes
    /// later.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasUserDefault))]
    private void LoadDefault()
    {
        if (_userDefault is null)
            return;

        var loaded = _userDefault.Copy();
        _settings.Views = loaded.Views;
        _settings.Opacity = loaded.Opacity;
        _settings.Order = loaded.Order;
        _settings.Colors = loaded.Colors;
        Load(_board, _settings, _orderSide);
        Changed?.Invoke();
    }

    /// <summary>Fills the list for a board; the automatic order is the one of the given side.</summary>
    public void Load(Board? board, ReportLayerSettings settings, BoardSide orderSide = BoardSide.Top)
    {
        _settings = settings;
        _board = board;
        _orderSide = orderSide;
        _isBuilding = true;
        try
        {
            var effective = Effective;
            var chosen = Views.ToDictionary(v => v.Key, v => (effective.LayersOf(v.Key) ?? v.Defaults).ToHashSet());
            Options.Clear();
            if (board is not null)
            {
                foreach (var name in Ordered(board, orderSide))
                {
                    var option = new ReportLayerOption(name, _display(name), Views)
                    {
                        Opacity = DefaultPercent(name),
                        Color = DefaultColor(name)
                    };
                    foreach (var toggle in option.Toggles)
                        toggle.IsVisible = chosen[toggle.View.Key].Contains(name);
                    option.ToggleChanged += OnToggleChanged;
                    option.PropertyChanged += (_, e) => OnOptionChanged(e.PropertyName);
                    Options.Add(option);
                }
            }
        }
        finally
        {
            _isBuilding = false;
        }

        OnPropertyChanged(nameof(HasOptions));
        OnPropertyChanged(nameof(HasCustomOrder));
    }

    /// <summary>Opacity of a layer without a choice in the settings: the user's default, else the built-in one.</summary>
    private int DefaultPercent(string name) =>
        Effective.Opacity?.TryGetValue(name, out var percent) == true ? percent : ReportLayerOption.DefaultPercent(name);

    private Color DefaultColor(string name) =>
        Effective.Colors?.TryGetValue(name, out var hex) == true && Color.TryParse(hex, out var color) ? color : ReportLayerOption.DefaultColor(name);

    /// <summary>Names in the current language, after the language changed.</summary>
    public void RefreshDisplay()
    {
        foreach (var option in Options)
            option.SetDisplay(_display(option.Name));
        foreach (var view in Views)
            view.RefreshLabel();
    }

    /// <summary>The stored order for the layers it names, the automatic order of the side for the others, below them.</summary>
    private IEnumerable<string> Ordered(Board board, BoardSide side)
    {
        var names = new List<string> { LayerColors.TopPads, LayerColors.BottomPads, LayerColors.Holes };
        names.AddRange(board.Layers);
        var automatic = BoardSvgRenderer.DefaultLayerStack(board, side, names);
        if (Effective.Order is not { Count: > 0 } stored)
            return automatic;

        var known = stored.Where(names.Contains).ToList();
        return known.Concat(automatic.Where(n => !known.Contains(n)));
    }

    private void OnToggleChanged(ReportLayerToggle toggle)
    {
        if (_isBuilding)
            return;

        var key = toggle.View.Key;
        var chosen = (Effective.LayersOf(key) ?? toggle.View.Defaults).ToHashSet();
        foreach (var option in Options)
        {
            if (option.IsVisibleIn(key))
                chosen.Add(option.Name);
            else
                chosen.Remove(option.Name);
        }

        _settings.SetLayers(key, chosen.Order(StringComparer.Ordinal).ToList());
        Changed?.Invoke();
    }

    private void OnOptionChanged(string? property)
    {
        if (_isBuilding)
            return;

        switch (property)
        {
            case nameof(ReportLayerOption.Opacity):
                _settings.Opacity = Differing(Effective.Opacity, o => o.Opacity, o => ReportLayerOption.DefaultPercent(o.Name));
                break;
            case nameof(ReportLayerOption.Color):
                _settings.Colors = Differing(Effective.Colors, o => $"#{o.Color.R:X2}{o.Color.G:X2}{o.Color.B:X2}",
                    o => $"#{ReportLayerOption.DefaultColor(o.Name).R:X2}{ReportLayerOption.DefaultColor(o.Name).G:X2}{ReportLayerOption.DefaultColor(o.Name).B:X2}");
                break;
            default:
                return;
        }

        Changed?.Invoke();
    }

    /// <summary>Only values that differ from the default are kept; layers of other boards keep theirs.</summary>
    private Dictionary<string, T>? Differing<T>(Dictionary<string, T>? before, Func<ReportLayerOption, T> value,
        Func<ReportLayerOption, T> defaultValue)
    {
        var values = new Dictionary<string, T>(before ?? []);
        foreach (var option in Options)
        {
            if (EqualityComparer<T>.Default.Equals(value(option), defaultValue(option)))
                values.Remove(option.Name);
            else
                values[option.Name] = value(option);
        }

        return values.Count > 0 ? values : null;
    }

    [RelayCommand]
    private void MoveUp(ReportLayerOption? option) => Move(option, -1);

    [RelayCommand]
    private void MoveDown(ReportLayerOption? option) => Move(option, 1);

    /// <summary>Moves a layer in the list; from then on the order of the list is the stored order.</summary>
    private void Move(ReportLayerOption? option, int step)
    {
        if (option is null)
            return;

        var index = Options.IndexOf(option);
        var target = index + step;
        if (index < 0 || target < 0 || target >= Options.Count)
            return;

        Options.Move(index, target);
        var order = Options.Select(o => o.Name).ToList();
        order.AddRange((Effective.Order ?? []).Where(n => !order.Contains(n)));
        _settings.Order = order;
        OnPropertyChanged(nameof(HasCustomOrder));
        Changed?.Invoke();
    }

    /// <summary>Back to the order of the user's default, else the automatic one: the list is sorted again.</summary>
    [RelayCommand]
    private void ResetOrder()
    {
        _settings.Order = UserDefault?.Order is { Count: > 0 } order ? [.. order] : null;
        Load(_board, _settings, _orderSide);
        Changed?.Invoke();
    }

    /// <summary>Colors and opacities back to the user's default, else the built-in ones.</summary>
    [RelayCommand]
    private void ResetStyle()
    {
        _settings.Opacity = null;
        _settings.Colors = null;
        _isBuilding = true;
        try
        {
            foreach (var option in Options)
            {
                option.Opacity = DefaultPercent(option.Name);
                option.Color = DefaultColor(option.Name);
            }
        }
        finally
        {
            _isBuilding = false;
        }

        Changed?.Invoke();
    }

    /// <summary>Every layer ("all") or none ("-") in every view.</summary>
    [RelayCommand]
    private void ShowAll(string? which)
    {
        var show = which != "-";
        foreach (var toggle in Options.SelectMany(o => o.Toggles))
            toggle.IsVisible = show;
    }
}