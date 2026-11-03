using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Pcb.Controls;
using ATLab.Pcb.Kicad;
using ATLab.Services;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

/// <summary>
/// The board view below the test steps: shows where the selected step connects to the board, follows the running
/// test, keeps the nets of failed steps marked and can show which nets the project tests at all.
/// </summary>
public partial class BoardViewerViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ProjectModel _project;
    private TestStepViewModel? _stepViewModel;
    private TestStep? _step;

    /// <summary>Nets of the steps that failed in the current or last run.</summary>
    private readonly HashSet<string> _failedNets = new(StringComparer.Ordinal);

    public BoardViewerViewModel(BoardService boardService, ISettingsService settingsService, ProjectModel project,
        ITestExecutor testExecutor)
    {
        BoardService = boardService;
        _settingsService = settingsService;
        _project = project;

        var settings = settingsService.Settings;
        _isVisible = settings.IsShowBoardView;
        _viewSide = settings.BoardViewSide == 1 ? BoardSide.Bottom : BoardSide.Top;

        boardService.Changed += () => Dispatcher.UIThread.Post(OnBoardChanged);
        boardService.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(BoardService.IsBoardChanged) or nameof(BoardService.LoadError) or nameof(BoardService.IsLoading))
                OnPropertyChanged(nameof(Notice));
        };
        project.Board.ConfigurationChanged += () => Dispatcher.UIThread.Post(Refresh);
        project.BoundaryScan.ConfigurationChanged += () => Dispatcher.UIThread.Post(Refresh);

        testExecutor.TestStarted += () => Dispatcher.UIThread.Post(() =>
        {
            _failedNets.Clear();
            Refresh();
        });
        testExecutor.StepCompleted += (_, step) =>
        {
            if (step.IsPassed)
                return;

            var nets = boardService.GetHighlight(step.TestStep).Nets.Select(n => n.Net).ToList();
            Dispatcher.UIThread.Post(() =>
            {
                foreach (var net in nets)
                    _failedNets.Add(net);
                Refresh();
            });
        };

        BuildLayerOptions();
        BuildRoleOptions();
        if (Avalonia.Application.Current is { } app)
            app.ActualThemeVariantChanged += (_, _) => UpdateTheme(app.ActualThemeVariant != Avalonia.Styling.ThemeVariant.Light);

        if (project.Board.HasBoard && boardService.Board is null)
            _ = boardService.LoadAsync();
    }

    public BoardService BoardService { get; }

    public Board? Board => BoardService.Board;

    // ----- Visibility and view -----

    [ObservableProperty] private bool _isVisible;

    partial void OnIsVisibleChanged(bool value)
    {
        _settingsService.Settings.IsShowBoardView = value;
        _settingsService.Save();
        if (value)
            Refresh();
    }

    [RelayCommand]
    private void ToggleVisible() => IsVisible = !IsVisible;

    /// <summary>Share of the step area's height taken by the board view, as the user left the splitter.</summary>
    public double HeightRatio
    {
        get => Math.Clamp(_settingsService.Settings.BoardViewHeightRatio, 0.1, 0.9);
        set
        {
            _settingsService.Settings.BoardViewHeightRatio = Math.Clamp(value, 0.1, 0.9);
            _settingsService.Save();
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTopView), nameof(IsBottomView))]
    private BoardSide _viewSide;

    partial void OnViewSideChanged(BoardSide value)
    {
        _settingsService.Settings.BoardViewSide = value == BoardSide.Bottom ? 1 : 0;
        _settingsService.Save();
    }

    public bool IsTopView
    {
        get => ViewSide == BoardSide.Top;
        set { if (value) ViewSide = BoardSide.Top; }
    }

    public bool IsBottomView
    {
        get => ViewSide == BoardSide.Bottom;
        set { if (value) ViewSide = BoardSide.Bottom; }
    }

    /// <summary>
    /// Whether the zones (planes, pours) of highlighted nets are drawn. Off by default: a plane touched by a net, e.g.
    /// GND, covers most of the board.
    /// </summary>
    public bool IsHighlightZones
    {
        get => _settingsService.Settings.IsBoardHighlightZones;
        set
        {
            if (_settingsService.Settings.IsBoardHighlightZones == value)
                return;
            _settingsService.Settings.IsBoardHighlightZones = value;
            _settingsService.Save();
            OnPropertyChanged();
        }
    }

    [RelayCommand]
    private void ToggleHighlightZones() => IsHighlightZones = !IsHighlightZones;

    /// <summary>Instead of the selected step: which nets all steps of the project reach.</summary>
    [ObservableProperty] private bool _isCoverageMode;

    partial void OnIsCoverageModeChanged(bool value) => Refresh();

    [RelayCommand]
    private void ToggleCoverage() => IsCoverageMode = !IsCoverageMode;

    // ----- What is shown -----

    [ObservableProperty] private IReadOnlyList<NetHighlight> _netHighlights = [];
    [ObservableProperty] private IReadOnlyList<PadHighlight> _padHighlights = [];
    [ObservableProperty] private IReadOnlyList<FootprintHighlight> _footprintHighlights = [];
    [ObservableProperty] private IReadOnlyList<IBoardMarker> _markers = [];
    [ObservableProperty] private IReadOnlyList<BoardLegendItem> _legend = [];

    /// <summary>Title above the board: the step, or the coverage.</summary>
    [ObservableProperty] private string _title = string.Empty;

    [ObservableProperty] private bool _isTitleFailed;

    public bool HasLegend => Legend.Count > 0;
    partial void OnLegendChanged(IReadOnlyList<BoardLegendItem> value) => OnPropertyChanged(nameof(HasLegend));

    /// <summary>Message instead of or above the board: no board, loading, error, or a changed board file.</summary>
    public string Notice
    {
        get
        {
            var l = LocalizationService.Instance;
            if (!_project.Board.HasBoard)
                return l.GetString("Board_NoBoard");
            if (BoardService.IsLoading)
                return l.GetString("Board_Loading");
            if (BoardService.LoadError.Length > 0)
                return l.GetString("Board_LoadFailed").Replace("{0}", BoardService.LoadError);
            if (BoardService.IsBoardChanged)
                return l.GetString("Board_ChangedNotice");
            return string.Empty;
        }
    }

    public bool HasNotice => Notice.Length > 0;
    public bool ShowBoard => Board is not null;
    public bool IsBoardChanged => BoardService.IsBoardChanged;

    /// <summary>The selected step changed (also while a test runs, so the view follows it).</summary>
    public void ShowStep(TestStepViewModel? step)
    {
        if (_step is not null)
            _step.PropertyChanged -= OnStepChanged;

        _stepViewModel = step;
        _step = step?.TestStep;

        if (_step is not null)
            _step.PropertyChanged += OnStepChanged;

        Refresh();
    }

    /// <summary>Routing edited in the step configurator.</summary>
    private void OnStepChanged(object? sender, PropertyChangedEventArgs e) => Dispatcher.UIThread.Post(Refresh);

    private void OnBoardChanged()
    {
        OnPropertyChanged(nameof(Board));
        OnPropertyChanged(nameof(ShowBoard));
        OnPropertyChanged(nameof(Notice));
        OnPropertyChanged(nameof(HasNotice));
        OnPropertyChanged(nameof(IsBoardChanged));
        BuildLayerOptions();
        Refresh();
    }

    /// <summary>Highlights of the selected step (or the coverage) and the marks of failed steps.</summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(Notice));
        OnPropertyChanged(nameof(HasNotice));
        OnPropertyChanged(nameof(IsBoardChanged));

        if (!IsVisible || Board is null)
        {
            Clear();
            return;
        }

        var l = LocalizationService.Instance;
        if (IsCoverageMode)
        {
            var coverage = BoardService.GetCoverage(_project.Modules.SelectMany(m => m.TestSteps), _hiddenRoles);
            NetHighlights = coverage.CoveredNets.Select(n => new NetHighlight(n, BoardColors.Covered)).ToList();
            PadHighlights = [];
            FootprintHighlights = [];
            Markers = [];
            Legend = [];
            var percent = coverage.TotalNets > 0 ? 100.0 * coverage.CoveredNets.Count / coverage.TotalNets : 0;
            Title = l.GetString("Board_CoverageTitle")
                .Replace("{0}", coverage.CoveredNets.Count.ToString())
                .Replace("{1}", coverage.TotalNets.ToString())
                .Replace("{2}", percent.ToString("0.#", System.Globalization.CultureInfo.CurrentCulture));
            IsTitleFailed = false;
            return;
        }

        var highlight = BoardService.GetHighlight(_step, _hiddenRoles);

        // Failed steps stay marked, under the nets of the current step
        var nets = _failedNets
            .Where(n => !_hiddenRoles.Contains(BoardRole.Failed) && highlight.Nets.All(h => h.Net != n))
            .Select(n => new NetHighlight(n, BoardColors.Failed, NetHighlightStyle.Outline))
            .Concat(highlight.Nets)
            .ToList();

        NetHighlights = nets;
        PadHighlights = highlight.Pads;
        FootprintHighlights = highlight.Footprints;
        Markers = highlight.Probes;
        Legend = highlight.Legend;
        IsTitleFailed = _stepViewModel is { IsExecuted: true, IsPassed: false };
        Title = _step is null
            ? string.Empty
            : $"{_step.Number} · {_step.Name}" + (HasNoConnection(highlight) ? $"  —  {l.GetString("Board_NoConnection")}" : string.Empty);
    }

    /// <summary>The step connects to nothing on the board, not only to roles that are hidden.</summary>
    private bool HasNoConnection(StepHighlight shown) =>
        shown.Legend.Count == 0 && (_hiddenRoles.Count == 0 || BoardService.GetHighlight(_step).Legend.Count == 0);

    private void Clear()
    {
        NetHighlights = [];
        PadHighlights = [];
        FootprintHighlights = [];
        Markers = [];
        Legend = [];
        Title = string.Empty;
    }

    /// <summary>Development mode: the current board file becomes the reference for the nets.</summary>
    [RelayCommand]
    private void AcceptBoardChange()
    {
        BoardService.AcceptBoardChange();
        OnPropertyChanged(nameof(Notice));
        OnPropertyChanged(nameof(HasNotice));
        OnPropertyChanged(nameof(IsBoardChanged));
    }

    // ----- Roles: what is shown and in which color -----

    private static readonly (BoardRole Role, string LabelKey)[] Roles =
    [
        (BoardRole.High, "Board_RoleHigh"),
        (BoardRole.Low, "Board_RoleLow"),
        (BoardRole.Stimulus, "Board_RoleStimulus"),
        (BoardRole.ExternalStimulus, "Board_RoleExternalStimulus"),
        (BoardRole.Interface, "Board_RoleInterface"),
        (BoardRole.BoundaryScan, "Board_RoleBoundaryScan"),
        (BoardRole.Failed, "Board_RoleFailed")
    ];

    private readonly HashSet<BoardRole> _hiddenRoles = [];

    /// <summary>The toggle buttons above the board, one per role.</summary>
    public ObservableCollection<BoardRoleOption> RoleOptions { get; } = [];

    private void BuildRoleOptions()
    {
        foreach (var name in _settingsService.Settings.BoardHiddenRoles ?? [])
            if (Enum.TryParse<BoardRole>(name, out var role))
                _hiddenRoles.Add(role);

        foreach (var (role, key) in Roles)
            RoleOptions.Add(new BoardRoleOption(role, key, !_hiddenRoles.Contains(role), this));
    }

    internal void OnRoleShownChanged(BoardRole role, bool isShown)
    {
        if (isShown)
            _hiddenRoles.Remove(role);
        else
            _hiddenRoles.Add(role);

        _settingsService.Settings.BoardHiddenRoles = _hiddenRoles.Count > 0 ? _hiddenRoles.Select(r => r.ToString()).ToList() : null;
        _settingsService.Save();
        Refresh();
    }

    /// <summary>A color of a role, chosen by the user; the default color is not stored.</summary>
    internal void OnRoleColorChanged(BoardRole role, Color color)
    {
        BoardColors.Set(role, color);
        var colors = _settingsService.Settings.BoardRoleColors ?? new Dictionary<string, string>();
        if (color == BoardColors.DefaultOf(role))
            colors.Remove(role.ToString());
        else
            colors[role.ToString()] = BoardRoleOption.ToHex(color);
        _settingsService.Settings.BoardRoleColors = colors.Count > 0 ? colors : null;
        Refresh();

        // The picker changes the color continuously while dragging; the file is written once it rests
        _saveColorsTimer ??= new DispatcherTimer(TimeSpan.FromMilliseconds(600), DispatcherPriority.Background, (_, _) =>
        {
            _saveColorsTimer!.Stop();
            _settingsService.Save();
        });
        _saveColorsTimer.Stop();
        _saveColorsTimer.Start();
    }

    private DispatcherTimer? _saveColorsTimer;

    // ----- Layers -----

    private static readonly string[] DefaultLayers = [ATLab.Pcb.Controls.LayerColors.TopPads, ATLab.Pcb.Controls.LayerColors.BottomPads, ATLab.Pcb.Controls.LayerColors.Holes, "F.SilkS", "B.SilkS"];

    public ObservableCollection<BoardLayerOption> LayerOptions { get; } = [];

    [ObservableProperty] private ISet<string>? _visibleLayers;

    /// <summary>Colors the user chose for KiCad layers, for the board view.</summary>
    [ObservableProperty] private IReadOnlyDictionary<string, Color>? _layerColors;

    private bool _isDarkLayers = true;

    private void BuildLayerOptions()
    {
        var visible = (_settingsService.Settings.BoardVisibleLayers ?? [.. DefaultLayers]).ToHashSet();
        var dark = _settingsService.Settings.IsDarkMode;
        _isDarkLayers = dark;
        LayerColors = ATLab.Pcb.Controls.LayerColors.Parse(_settingsService.Settings.BoardLayerColors);
        LayerOptions.Clear();

        var names = new List<string> { ATLab.Pcb.Controls.LayerColors.TopPads, ATLab.Pcb.Controls.LayerColors.BottomPads, ATLab.Pcb.Controls.LayerColors.Holes };
        names.AddRange(Board?.Layers ?? []);
        foreach (var name in names)
        {
            var option = new BoardLayerOption(name, dark) { IsVisible = visible.Contains(name) };
            ApplyColors(option, dark, LayerColors);
            option.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(BoardLayerOption.IsVisible))
                    OnLayerVisibilityChanged();
                else if (e.PropertyName == nameof(BoardLayerOption.Color) && !option.IsSettingColor)
                    OnLayerColorChanged(option.Name, option.Color);
            };
            LayerOptions.Add(option);
        }

        VisibleLayers = visible;
    }

    private void OnLayerVisibilityChanged()
    {
        // Layers this board does not have keep their setting for other boards
        var visible = (_settingsService.Settings.BoardVisibleLayers ?? [.. DefaultLayers]).ToHashSet();
        foreach (var option in LayerOptions)
        {
            if (option.IsVisible)
                visible.Add(option.Name);
            else
                visible.Remove(option.Name);
        }

        VisibleLayers = visible;
        _settingsService.Settings.BoardVisibleLayers = visible.Order().ToList();
        _settingsService.Save();
    }

    [RelayCommand]
    private void ShowAllLayers(bool visible)
    {
        foreach (var option in LayerOptions)
            option.IsVisible = visible || option.Name is ATLab.Pcb.Controls.LayerColors.TopPads or ATLab.Pcb.Controls.LayerColors.BottomPads or ATLab.Pcb.Controls.LayerColors.Holes;
    }

    /// <summary>Colors of the layer list follow the theme; chosen colors stay.</summary>
    public void UpdateTheme(bool dark)
    {
        _isDarkLayers = dark;
        foreach (var option in LayerOptions)
            ApplyColors(option, dark, LayerColors);
    }

    /// <summary>A layer color chosen by the user, kept in the app settings; the default color is not stored.</summary>
    private void OnLayerColorChanged(string key, Avalonia.Media.Color color)
    {
        var colors = _settingsService.Settings.BoardLayerColors ?? new Dictionary<string, string>();
        if (color == ATLab.Pcb.Controls.LayerColors.Get(key, _isDarkLayers))
            colors.Remove(key);
        else
            colors[key] = ATLab.Pcb.Controls.LayerColors.ToHex(color);
        _settingsService.Settings.BoardLayerColors = colors.Count > 0 ? colors : null;
        LayerColors = ATLab.Pcb.Controls.LayerColors.Parse(_settingsService.Settings.BoardLayerColors);

        // The picker changes the color continuously while dragging; the file is written once it rests
        _saveLayerColorsTimer ??= new DispatcherTimer(TimeSpan.FromMilliseconds(600), DispatcherPriority.Background, (_, _) =>
        {
            _saveLayerColorsTimer!.Stop();
            _settingsService.Save();
        });
        _saveLayerColorsTimer.Stop();
        _saveLayerColorsTimer.Start();
    }

    private DispatcherTimer? _saveLayerColorsTimer;

    /// <summary>The color of a layer from the user's choice, else the theme's.</summary>
    private static void ApplyColors(BoardLayerOption option, bool dark, IReadOnlyDictionary<string, Avalonia.Media.Color>? chosen) =>
        option.SetColor(ATLab.Pcb.Controls.LayerColors.Get(option.Name, dark, chosen));

    /// <summary>All layers back to their default colors.</summary>
    [RelayCommand]
    private void ResetLayerColors()
    {
        _settingsService.Settings.BoardLayerColors = null;
        _settingsService.Save();
        LayerColors = null;
        foreach (var option in LayerOptions)
            ApplyColors(option, _isDarkLayers, null);
    }
}

/// <summary>Layer in the layer list of the board view.</summary>
public partial class BoardLayerOption(string name, bool dark) : ObservableObject
{
    public string Name { get; } = name;

    public string Display => Name switch
    {
        ATLab.Pcb.Controls.LayerColors.TopPads => LocalizationService.Instance.GetString("Board_LayerTopPads"),
        ATLab.Pcb.Controls.LayerColors.BottomPads => LocalizationService.Instance.GetString("Board_LayerBottomPads"),
        ATLab.Pcb.Controls.LayerColors.Holes => LocalizationService.Instance.GetString("Board_LayerHoles"),
        _ => Name
    };

    [ObservableProperty] private bool _isVisible;

    /// <summary>Color of the layer in the board view; the color picker sets it, only for KiCad layers.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Brush))]
    private Color _color = ATLab.Pcb.Controls.LayerColors.Get(name, dark);

    public IBrush Brush => new SolidColorBrush(Color);

    /// <summary>True while the color comes from the theme or the settings, not from the user.</summary>
    public bool IsSettingColor { get; private set; }

    /// <summary>A color from the theme or the settings, not a choice of the user.</summary>
    public void SetColor(Color color)
    {
        IsSettingColor = true;
        try
        {
            Color = color;
        }
        finally
        {
            IsSettingColor = false;
        }
    }

    partial void OnColorChanged(Color value)
    {
        if (value.A != 255)
            Color = Color.FromRgb(value.R, value.G, value.B);
    }
}

/// <summary>
/// A role above the board view (MEAS H, STIM, interface …): shown or hidden with a click, its color chosen with the
/// color picker.
/// </summary>
public sealed partial class BoardRoleOption : ObservableObject
{
    private readonly BoardViewerViewModel _owner;
    private readonly string _labelKey;

    public BoardRoleOption(BoardRole role, string labelKey, bool isShown, BoardViewerViewModel owner)
    {
        Role = role;
        _labelKey = labelKey;
        _owner = owner;
        _isShown = isShown;
        _color = BoardColors.Of(role);
        LocalizationService.Instance.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Label));
    }

    public BoardRole Role { get; }

    public string Label => LocalizationService.Instance.GetString(_labelKey);

    [ObservableProperty] private bool _isShown;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Brush))]
    private Color _color;

    public IBrush Brush => new SolidColorBrush(Color);

    partial void OnIsShownChanged(bool value) => _owner.OnRoleShownChanged(Role, value);

    // Colors on the board are opaque
    partial void OnColorChanged(Color value)
    {
        if (value.A != 255)
            Color = Avalonia.Media.Color.FromRgb(value.R, value.G, value.B);
        else
            _owner.OnRoleColorChanged(Role, value);
    }

    [RelayCommand]
    private void Toggle() => IsShown = !IsShown;

    [RelayCommand]
    private void ResetColor() => Color = BoardColors.DefaultOf(Role);

    public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}