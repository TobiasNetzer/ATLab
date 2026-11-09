using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using ATLab.Pcb.Kicad;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace ATLab.Pcb.Controls;

/// <summary>
/// Drawing of a KiCad board with its layers, pads and holes, seen from the top or from the bottom (mirrored). On top
/// of it: highlighted nets, pads and components, and markers such as probes and tooling pins. Mouse wheel zooms,
/// dragging pans, a click selects a pad, a double-click on a pad or hole runs a command, a double-click on the
/// background fits the board into the view.
/// </summary>
public sealed class BoardView : Control
{
    public static readonly StyledProperty<Board?> BoardProperty =
        AvaloniaProperty.Register<BoardView, Board?>(nameof(Board));

    public static readonly StyledProperty<BoardSide> ViewSideProperty =
        AvaloniaProperty.Register<BoardView, BoardSide>(nameof(ViewSide));

    /// <summary>Probes and similar markers, numbered and placed so their labels do not overlap.</summary>
    public static readonly StyledProperty<IReadOnlyList<IBoardMarker>?> MarkersProperty =
        AvaloniaProperty.Register<BoardView, IReadOnlyList<IBoardMarker>?>(nameof(Markers));

    /// <summary>Further markers, e.g. tooling pins, kept in their own list by the owner.</summary>
    /// <summary>Parts the user places freely (pre-centering pins, support pins, push rods); they can be dragged.</summary>
    public static readonly StyledProperty<IReadOnlyList<IBoardMarker>?> PartMarkersProperty =
        AvaloniaProperty.Register<BoardView, IReadOnlyList<IBoardMarker>?>(nameof(PartMarkers));

    public static readonly StyledProperty<IBoardMarker?> SelectedPartMarkerProperty =
        AvaloniaProperty.Register<BoardView, IBoardMarker?>(nameof(SelectedPartMarker), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>
    /// Colors of the kinds of parts, the same as in the build sheet; warning orange and error red stay for the results
    /// of the checks.
    /// </summary>
    public static readonly StyledProperty<IBrush> ToolingPinBrushProperty =
        AvaloniaProperty.Register<BoardView, IBrush>(nameof(ToolingPinBrush), new SolidColorBrush(Color.Parse(MarkerColors.ToolingPin)));

    public static readonly StyledProperty<IBrush> PreCenteringPinBrushProperty =
        AvaloniaProperty.Register<BoardView, IBrush>(nameof(PreCenteringPinBrush), new SolidColorBrush(Color.Parse(MarkerColors.PreCenteringPin)));

    public static readonly StyledProperty<IBrush> SupportPinBrushProperty =
        AvaloniaProperty.Register<BoardView, IBrush>(nameof(SupportPinBrush), new SolidColorBrush(Color.Parse(MarkerColors.SupportPin)));

    public static readonly StyledProperty<IBrush> PushRodBrushProperty =
        AvaloniaProperty.Register<BoardView, IBrush>(nameof(PushRodBrush), new SolidColorBrush(Color.Parse(MarkerColors.PushRod)));

    /// <summary>
    /// A second legend line with the kinds of markers on the board (probes, tooling pins, parts, plate outline), each
    /// with its symbol and color. For views whose markers have fixed colors, like the Fixture Designer's.
    /// </summary>
    public static readonly StyledProperty<bool> ShowMarkerLegendProperty =
        AvaloniaProperty.Register<BoardView, bool>(nameof(ShowMarkerLegend));

    /// <summary>While true, a click places a part: <see cref="PlaceCommand"/> gets the board point (mm).</summary>
    public static readonly StyledProperty<bool> IsPlacementActiveProperty =
        AvaloniaProperty.Register<BoardView, bool>(nameof(IsPlacementActive));

    public static readonly StyledProperty<ICommand?> PlaceCommandProperty =
        AvaloniaProperty.Register<BoardView, ICommand?>(nameof(PlaceCommand));

    /// <summary>A part marker was dragged to a new place; the parameter is the marker.</summary>
    public static readonly StyledProperty<ICommand?> PartMovedCommandProperty =
        AvaloniaProperty.Register<BoardView, ICommand?>(nameof(PartMovedCommand));


    public static readonly StyledProperty<IBoardMarker?> SelectedMarkerProperty =
        AvaloniaProperty.Register<BoardView, IBoardMarker?>(nameof(SelectedMarker));

    /// <summary>
    /// Whether the zones (planes, pours) of highlighted nets are drawn too. A ground plane covers most of the board, so
    /// it is off by default.
    /// </summary>
    public static readonly StyledProperty<bool> IsZoneHighlightEnabledProperty =
        AvaloniaProperty.Register<BoardView, bool>(nameof(IsZoneHighlightEnabled));

    public static readonly StyledProperty<IReadOnlyList<NetHighlight>?> NetHighlightsProperty =
        AvaloniaProperty.Register<BoardView, IReadOnlyList<NetHighlight>?>(nameof(NetHighlights));

    public static readonly StyledProperty<IReadOnlyList<PadHighlight>?> PadHighlightsProperty =
        AvaloniaProperty.Register<BoardView, IReadOnlyList<PadHighlight>?>(nameof(PadHighlights));

    public static readonly StyledProperty<IReadOnlyList<FootprintHighlight>?> FootprintHighlightsProperty =
        AvaloniaProperty.Register<BoardView, IReadOnlyList<FootprintHighlight>?>(nameof(FootprintHighlights));

    /// <summary>Pads shown in full color; the others are drawn faint. Null shows all pads.</summary>
    public static readonly StyledProperty<ISet<Pad>?> HighlightedPadsProperty =
        AvaloniaProperty.Register<BoardView, ISet<Pad>?>(nameof(HighlightedPads));

    public static readonly StyledProperty<Pad?> SelectedPadProperty =
        AvaloniaProperty.Register<BoardView, Pad?>(nameof(SelectedPad), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Executed with the pad on a double-click.</summary>
    public static readonly StyledProperty<ICommand?> PadActivatedCommandProperty =
        AvaloniaProperty.Register<BoardView, ICommand?>(nameof(PadActivatedCommand));

    /// <summary>Executed with the hole on a double-click on an unplated hole.</summary>

    /// <summary>KiCad layers and the pseudo layers of <see cref="LayerColors"/> that are drawn.</summary>
    public static readonly StyledProperty<ISet<string>?> VisibleLayersProperty =
        AvaloniaProperty.Register<BoardView, ISet<string>?>(nameof(VisibleLayers));

    /// <summary>Colors the user chose for KiCad layers; the others keep their default color.</summary>
    public static readonly StyledProperty<IReadOnlyDictionary<string, Color>?> LayerColorsProperty =
        AvaloniaProperty.Register<BoardView, IReadOnlyDictionary<string, Color>?>(nameof(LayerColors));

    /// <summary>Key of the help text at the bottom (see <see cref="BoardTexts"/>); empty for none.</summary>
    public static readonly StyledProperty<string> HintKeyProperty =
        AvaloniaProperty.Register<BoardView, string>(nameof(HintKey), "Board_Hint");

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<BoardView, IBrush?>(nameof(Background), Brushes.Black);

    public static readonly StyledProperty<IBrush> OutlineBrushProperty =
        AvaloniaProperty.Register<BoardView, IBrush>(nameof(OutlineBrush), Brushes.LightGray);

    public static readonly StyledProperty<IBrush> TopPadBrushProperty =
        AvaloniaProperty.Register<BoardView, IBrush>(nameof(TopPadBrush), Brushes.Orange);

    public static readonly StyledProperty<IBrush> BottomPadBrushProperty =
        AvaloniaProperty.Register<BoardView, IBrush>(nameof(BottomPadBrush), Brushes.CornflowerBlue);

    public static readonly StyledProperty<IBrush> ForegroundProperty =
        AvaloniaProperty.Register<BoardView, IBrush>(nameof(Foreground), Brushes.White);

    public static readonly StyledProperty<IBrush> MutedBrushProperty =
        AvaloniaProperty.Register<BoardView, IBrush>(nameof(MutedBrush), Brushes.Gray);

    /// <summary>
    /// Color of the clicked pad and its net; null for the foreground (white on the dark board, near black on the light
    /// one), which no role or pad color comes close to.
    /// </summary>
    /// <summary>
    /// Outlines drawn over the board (board coordinates, mm), dashed, e.g. the outline of a probe plate, so its
    /// position against the board can be seen.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyList<IReadOnlyList<Point2>>?> OverlayOutlinesProperty =
        AvaloniaProperty.Register<BoardView, IReadOnlyList<IReadOnlyList<Point2>>?>(nameof(OverlayOutlines));

    /// <summary>Areas of the overlay shaded in the overlay color, e.g. the pockets of a plate (closed, board coordinates).</summary>
    public static readonly StyledProperty<IReadOnlyList<IReadOnlyList<Point2>>?> OverlayFillsProperty =
        AvaloniaProperty.Register<BoardView, IReadOnlyList<IReadOnlyList<Point2>>?>(nameof(OverlayFills));

    /// <summary>Lines of the overlay drawn thin and solid, e.g. lines of an imported drawing that belong to no shape.</summary>
    public static readonly StyledProperty<IReadOnlyList<IReadOnlyList<Point2>>?> OverlayGuidesProperty =
        AvaloniaProperty.Register<BoardView, IReadOnlyList<IReadOnlyList<Point2>>?>(nameof(OverlayGuides));

    /// <summary>A line of the overlay drawn wide in the selection color, e.g. the shape chosen in a list.</summary>
    public static readonly StyledProperty<IReadOnlyList<Point2>?> OverlayHighlightProperty =
        AvaloniaProperty.Register<BoardView, IReadOnlyList<Point2>?>(nameof(OverlayHighlight));

    /// <summary>Color of the overlay outlines; null for the foreground color.</summary>
    public static readonly StyledProperty<IBrush?> OverlayBrushProperty =
        AvaloniaProperty.Register<BoardView, IBrush?>(nameof(OverlayBrush));

    /// <summary>Rounded corners of the view's background, to follow a rounded border around it.</summary>
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<BoardView, CornerRadius>(nameof(CornerRadius));

    public static readonly StyledProperty<IBrush?> SelectionBrushProperty =
        AvaloniaProperty.Register<BoardView, IBrush?>(nameof(SelectionBrush));

    public static readonly StyledProperty<IBrush> AccentBrushProperty =
        AvaloniaProperty.Register<BoardView, IBrush>(nameof(AccentBrush), Brushes.DeepSkyBlue);

    public static readonly StyledProperty<IBrush> ProbeBrushProperty =
        AvaloniaProperty.Register<BoardView, IBrush>(nameof(ProbeBrush), Brushes.LimeGreen);

    public static readonly StyledProperty<IBrush> ErrorBrushProperty =
        AvaloniaProperty.Register<BoardView, IBrush>(nameof(ErrorBrush), Brushes.Red);

    public static readonly StyledProperty<IBrush> WarningBrushProperty =
        AvaloniaProperty.Register<BoardView, IBrush>(nameof(WarningBrush), Brushes.Orange);

    private const double ViewMargin = 24;
    private const double FontSize = 11;

    /// <summary>Opacity of the board under solid highlights, so the highlighted nets stand out.</summary>
    private const double DimmedOpacity = 0.35;

    private static readonly Typeface Typeface = new(FontFamily.Default);

    // View: scale in pixels per mm and the shift of the board center, both changed by zoom and pan
    private double _scale = 1;
    private Vector _pan;
    private bool _isUserView;

    private Point? _dragStart;

    /// <summary>The part marker being dragged, if the drag started on one.</summary>
    private IMovableBoardMarker? _draggedPart;
    private Vector _panAtDragStart;
    private bool _isDragging;
    private Pad? _hoveredPad;
    private Hole? _hoveredHole;

    // Collections and items the view redraws for
    private readonly Dictionary<StyledProperty<IReadOnlyList<IBoardMarker>?>, (INotifyCollectionChanged? Collection, List<IBoardMarker> Items)> _observed = [];

    static BoardView()
    {
        AffectsRender<BoardView>(
            BoardProperty, ViewSideProperty, MarkersProperty, SelectedMarkerProperty,
            PartMarkersProperty, SelectedPartMarkerProperty, IsPlacementActiveProperty, ShowMarkerLegendProperty,
            ToolingPinBrushProperty, PreCenteringPinBrushProperty, SupportPinBrushProperty, PushRodBrushProperty, OverlayBrushProperty,
            NetHighlightsProperty, IsZoneHighlightEnabledProperty, PadHighlightsProperty, FootprintHighlightsProperty, HighlightedPadsProperty,
            SelectedPadProperty, VisibleLayersProperty, HintKeyProperty, BackgroundProperty, OutlineBrushProperty,
            TopPadBrushProperty, BottomPadBrushProperty, ForegroundProperty, MutedBrushProperty, AccentBrushProperty, SelectionBrushProperty, CornerRadiusProperty, OverlayOutlinesProperty,
            OverlayFillsProperty, OverlayGuidesProperty, OverlayHighlightProperty, ProbeBrushProperty, ErrorBrushProperty, WarningBrushProperty);
        FocusableProperty.OverrideDefaultValue<BoardView>(true);
        ClipToBoundsProperty.OverrideDefaultValue<BoardView>(true);
    }

    public BoardView()
    {
        // Layer colors depend on the theme
        ActualThemeVariantChanged += (_, _) =>
        {
            _layers = null;
            InvalidateVisual();
        };
    }

    public Board? Board { get => GetValue(BoardProperty); set => SetValue(BoardProperty, value); }
    public BoardSide ViewSide { get => GetValue(ViewSideProperty); set => SetValue(ViewSideProperty, value); }
    public IReadOnlyList<IBoardMarker>? Markers { get => GetValue(MarkersProperty); set => SetValue(MarkersProperty, value); }
    public IReadOnlyList<IBoardMarker>? PartMarkers { get => GetValue(PartMarkersProperty); set => SetValue(PartMarkersProperty, value); }
    public IBoardMarker? SelectedPartMarker { get => GetValue(SelectedPartMarkerProperty); set => SetValue(SelectedPartMarkerProperty, value); }
    public bool IsPlacementActive { get => GetValue(IsPlacementActiveProperty); set => SetValue(IsPlacementActiveProperty, value); }
    public IBrush ToolingPinBrush { get => GetValue(ToolingPinBrushProperty); set => SetValue(ToolingPinBrushProperty, value); }
    public IBrush PreCenteringPinBrush { get => GetValue(PreCenteringPinBrushProperty); set => SetValue(PreCenteringPinBrushProperty, value); }
    public IBrush SupportPinBrush { get => GetValue(SupportPinBrushProperty); set => SetValue(SupportPinBrushProperty, value); }
    public IBrush PushRodBrush { get => GetValue(PushRodBrushProperty); set => SetValue(PushRodBrushProperty, value); }
    public bool ShowMarkerLegend { get => GetValue(ShowMarkerLegendProperty); set => SetValue(ShowMarkerLegendProperty, value); }
    public ICommand? PlaceCommand { get => GetValue(PlaceCommandProperty); set => SetValue(PlaceCommandProperty, value); }
    public ICommand? PartMovedCommand { get => GetValue(PartMovedCommandProperty); set => SetValue(PartMovedCommandProperty, value); }
    public IBoardMarker? SelectedMarker { get => GetValue(SelectedMarkerProperty); set => SetValue(SelectedMarkerProperty, value); }
    public bool IsZoneHighlightEnabled { get => GetValue(IsZoneHighlightEnabledProperty); set => SetValue(IsZoneHighlightEnabledProperty, value); }
    public IReadOnlyList<NetHighlight>? NetHighlights { get => GetValue(NetHighlightsProperty); set => SetValue(NetHighlightsProperty, value); }
    public IReadOnlyList<PadHighlight>? PadHighlights { get => GetValue(PadHighlightsProperty); set => SetValue(PadHighlightsProperty, value); }
    public IReadOnlyList<FootprintHighlight>? FootprintHighlights { get => GetValue(FootprintHighlightsProperty); set => SetValue(FootprintHighlightsProperty, value); }
    public ISet<Pad>? HighlightedPads { get => GetValue(HighlightedPadsProperty); set => SetValue(HighlightedPadsProperty, value); }
    public Pad? SelectedPad { get => GetValue(SelectedPadProperty); set => SetValue(SelectedPadProperty, value); }
    public ICommand? PadActivatedCommand { get => GetValue(PadActivatedCommandProperty); set => SetValue(PadActivatedCommandProperty, value); }
    public ISet<string>? VisibleLayers { get => GetValue(VisibleLayersProperty); set => SetValue(VisibleLayersProperty, value); }
    public IReadOnlyDictionary<string, Color>? LayerColors { get => GetValue(LayerColorsProperty); set => SetValue(LayerColorsProperty, value); }
    public string HintKey { get => GetValue(HintKeyProperty); set => SetValue(HintKeyProperty, value); }
    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public IBrush OutlineBrush { get => GetValue(OutlineBrushProperty); set => SetValue(OutlineBrushProperty, value); }
    public IBrush TopPadBrush { get => GetValue(TopPadBrushProperty); set => SetValue(TopPadBrushProperty, value); }
    public IBrush BottomPadBrush { get => GetValue(BottomPadBrushProperty); set => SetValue(BottomPadBrushProperty, value); }
    public IBrush Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }
    public IBrush MutedBrush { get => GetValue(MutedBrushProperty); set => SetValue(MutedBrushProperty, value); }
    public IBrush AccentBrush { get => GetValue(AccentBrushProperty); set => SetValue(AccentBrushProperty, value); }
    public IBrush? SelectionBrush { get => GetValue(SelectionBrushProperty); set => SetValue(SelectionBrushProperty, value); }
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public IReadOnlyList<IReadOnlyList<Point2>>? OverlayOutlines { get => GetValue(OverlayOutlinesProperty); set => SetValue(OverlayOutlinesProperty, value); }
    public IReadOnlyList<IReadOnlyList<Point2>>? OverlayFills { get => GetValue(OverlayFillsProperty); set => SetValue(OverlayFillsProperty, value); }
    public IReadOnlyList<IReadOnlyList<Point2>>? OverlayGuides { get => GetValue(OverlayGuidesProperty); set => SetValue(OverlayGuidesProperty, value); }
    public IReadOnlyList<Point2>? OverlayHighlight { get => GetValue(OverlayHighlightProperty); set => SetValue(OverlayHighlightProperty, value); }
    public IBrush ProbeBrush { get => GetValue(ProbeBrushProperty); set => SetValue(ProbeBrushProperty, value); }
    public IBrush? OverlayBrush { get => GetValue(OverlayBrushProperty); set => SetValue(OverlayBrushProperty, value); }
    public IBrush ErrorBrush { get => GetValue(ErrorBrushProperty); set => SetValue(ErrorBrushProperty, value); }
    public IBrush WarningBrush { get => GetValue(WarningBrushProperty); set => SetValue(WarningBrushProperty, value); }

    /// <summary>Shows the whole board again.</summary>
    public void FitToView()
    {
        _isUserView = false;
        _pan = default;
        InvalidateVisual();
    }

    // ----- Keeping up with the bound data -----

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LayerColorsProperty)
        {
            // The layers are cached with their colors
            _layers = null;
            InvalidateVisual();
        }

        if (change.Property == BoardProperty)
        {
            _hoveredPad = null;
            _hoveredHole = null;
            _layers = null;
            _nets = null;
            FitToView();
        }
        else if (change.Property == MarkersProperty || change.Property == PartMarkersProperty)
        {
            Observe((StyledProperty<IReadOnlyList<IBoardMarker>?>)change.Property);
        }
        else if (change.Property == IsPlacementActiveProperty)
        {
            Cursor = IsPlacementActive ? new Cursor(StandardCursorType.Cross) : Cursor.Default;
        }
        else if (change.Property == ViewSideProperty)
        {
            // Mirroring the view keeps the same area visible
            _pan = new Vector(-_pan.X, _pan.Y);
        }
        else if (change.Property == SelectedMarkerProperty && SelectedMarker?.MarkerPosition is { } position)
        {
            BringIntoView(position);
        }
        else if (change.Property == SelectedPadProperty && SelectedPad is { } selected)
        {
            BringIntoView(selected.Position);
        }
    }

    /// <summary>Redraws when markers are added, removed or changed.</summary>
    private void Observe(StyledProperty<IReadOnlyList<IBoardMarker>?> property)
    {
        if (_observed.TryGetValue(property, out var old))
        {
            if (old.Collection is not null)
                old.Collection.CollectionChanged -= OnMarkersChanged;
            foreach (var item in old.Items)
                item.PropertyChanged -= OnMarkerChanged;
        }

        var list = GetValue(property);
        var collection = list as INotifyCollectionChanged;
        if (collection is not null)
            collection.CollectionChanged += OnMarkersChanged;

        var items = list?.ToList() ?? [];
        foreach (var item in items)
            item.PropertyChanged += OnMarkerChanged;

        _observed[property] = (collection, items);
        InvalidateVisual();
    }

    private void OnMarkersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var (property, observed) in _observed.ToList())
            if (ReferenceEquals(observed.Collection, sender))
                Observe(property);
    }

    private void OnMarkerChanged(object? sender, PropertyChangedEventArgs e) => InvalidateVisual();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        BoardTexts.Changed += OnTextsChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        BoardTexts.Changed -= OnTextsChanged;
    }

    private void OnTextsChanged(object? sender, EventArgs e) => InvalidateVisual();

    // ----- Coordinates -----

    private double FitScale(Bounds2 bounds)
    {
        var width = Math.Max(1, Bounds.Width - 2 * ViewMargin);
        var height = Math.Max(1, Bounds.Height - 2 * ViewMargin);
        return Math.Min(width / Math.Max(bounds.Width, 1), height / Math.Max(bounds.Height, 1));
    }

    private double Mirror => ViewSide == BoardSide.Bottom ? -1 : 1;

    /// <summary>What the view shows when fitted: the board, and the overlay outlines where they reach beyond it.</summary>
    private Bounds2 ViewArea(Board board)
    {
        var area = board.Bounds;
        if (OverlayOutlines is not { Count: > 0 } outlines)
            return area;

        var points = outlines.SelectMany(o => o).ToList();
        return points.Count == 0
            ? area
            : new Bounds2(Math.Min(area.MinX, points.Min(p => p.X)), Math.Min(area.MinY, points.Min(p => p.Y)),
                Math.Max(area.MaxX, points.Max(p => p.X)), Math.Max(area.MaxY, points.Max(p => p.Y)));
    }

    /// <summary>Board millimeters (KiCad coordinates) to pixels of the control.</summary>
    private Matrix ViewMatrix(Board board)
    {
        var center = ViewArea(board).Center;
        return Matrix.CreateTranslation(-center.X, -center.Y) *
               Matrix.CreateScale(Mirror * _scale, _scale) *
               Matrix.CreateTranslation(Bounds.Width / 2 + _pan.X, Bounds.Height / 2 + _pan.Y);
    }

    private Point ToScreen(Board board, Point2 p) => new Point(p.X, p.Y).Transform(ViewMatrix(board));

    private Point2 ToBoard(Board board, Point screen)
    {
        var inverse = ViewMatrix(board).Invert();
        var p = screen.Transform(inverse);
        return new Point2(p.X, p.Y);
    }

    private static Point ToScreenPoint(Point2 p, Matrix view) => new Point(p.X, p.Y).Transform(view);

    private void BringIntoView(Point2 position)
    {
        if (Board is not { } board || Bounds.Width <= 0)
            return;

        var screen = ToScreen(board, position);
        var area = new Rect(Bounds.Size).Deflate(ViewMargin);
        if (area.Contains(screen))
            return;

        _pan += new Vector(Bounds.Width / 2 - screen.X, Bounds.Height / 2 - screen.Y);
        _isUserView = true;
        InvalidateVisual();
    }

    // ----- Drawing -----

    public override void Render(DrawingContext context)
    {
        // Background and drawing inside the rounded corners, e.g. those of the panel around the view
        var area = new RoundedRect(new Rect(Bounds.Size), CornerRadius);
        using (context.PushClip(area))
        {
            context.DrawRectangle(Background ?? Brushes.Transparent, null, area);
            RenderBoard(context);
        }
    }

    private void RenderBoard(DrawingContext context)
    {
        if (Board is not { } board)
            return;

        if (!_isUserView)
            _scale = FitScale(ViewArea(board));

        var view = ViewMatrix(board);
        // Any highlight dims the rest of the board, also outlines alone (e.g. only the nets of failed steps)
        var isDimmed = NetHighlights is { Count: > 0 } || PadHighlights is { Count: > 0 } || FootprintHighlights is { Count: > 0 };

        using (context.PushOpacity(isDimmed ? DimmedOpacity : 1))
        {
            DrawLayers(context, board, view);
            DrawOutline(context, board, view);
            if (IsLayerVisible(Controls.LayerColors.Holes))
                DrawHoles(context, board, view);
            DrawPads(context, board, view);
        }

        DrawNetHighlights(context, board, view);
        DrawOverlayOutlines(context, view);
        DrawFootprintHighlights(context, board, view);
        DrawPadHighlights(context, view);
        DrawSelection(context, board, view);
        DrawPartMarkers(context, view);
        DrawMarkers(context, view);
        DrawLegend(context);
        DrawHoverInfo(context);
    }

    private void DrawOutline(DrawingContext context, Board board, Matrix view)
    {
        var pen = new Pen(OutlineBrush, 1.5);
        foreach (var polyline in board.Outline)
        {
            var geometry = new StreamGeometry();
            using (var g = geometry.Open())
            {
                g.BeginFigure(ToScreenPoint(polyline[0], view), false);
                for (var i = 1; i < polyline.Count; i++)
                    g.LineTo(ToScreenPoint(polyline[i], view));
                g.EndFigure(false);
            }

            context.DrawGeometry(null, pen, geometry);
        }
    }

    private bool IsLayerVisible(string layer) =>
        VisibleLayers?.Contains(layer) ?? layer is Controls.LayerColors.TopPads or Controls.LayerColors.BottomPads or Controls.LayerColors.Holes;

    private BoardSide FarSide => ViewSide == BoardSide.Top ? BoardSide.Bottom : BoardSide.Top;

    // ----- KiCad layers -----

    /// <summary>Geometry of a layer in board coordinates, built once per board and theme.</summary>
    private sealed class LayerGeometry
    {
        public required string Name { get; init; }
        public required Color Color { get; init; }
        public required BoardSide? Side { get; init; }
        public List<(Pen Pen, StreamGeometry Geometry)> Strokes { get; } = [];
        public StreamGeometry? Fill { get; set; }
        public List<(FormattedText Text, Matrix Transform)> Texts { get; } = [];
    }

    private List<LayerGeometry>? _layers;

    private List<LayerGeometry> BuildLayers(Board board)
    {
        var dark = ActualThemeVariant != Avalonia.Styling.ThemeVariant.Light;
        var result = new List<LayerGeometry>();

        foreach (var name in board.Layers)
        {
            var color = Controls.LayerColors.Get(name, dark, LayerColors);
            var layer = new LayerGeometry { Name = name, Color = color, Side = Board.SideOfLayer(name) };
            var brush = new SolidColorBrush(color);
            var graphics = board.Graphics.Where(g => g.Layer == name).ToList();

            foreach (var (pen, geometry) in StrokeGeometries(graphics.Where(g => !g.Filled), brush))
                layer.Strokes.Add((pen, geometry));
            layer.Fill = FillGeometry(graphics.Where(g => g.Filled));

            foreach (var text in board.Texts.Where(t => t.Layer == name))
            {
                // The KiCad text size is about the height of capitals; a font needs a larger em size for that
                var formatted = new FormattedText(text.Text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    Typeface, Math.Max(text.Height * 1.4, 0.1), brush);
                var x = text.HorizontalAlignment switch { -1 => 0, 1 => -formatted.Width, _ => -formatted.Width / 2 };
                var y = text.VerticalAlignment switch { -1 => 0, 1 => -formatted.Height, _ => -formatted.Height / 2 };
                var transform = Matrix.CreateTranslation(x, y) *
                                Matrix.CreateScale(text.Mirrored ? -1 : 1, 1) *
                                Matrix.CreateRotation(-text.Angle * Math.PI / 180) *
                                Matrix.CreateTranslation(text.Position.X, text.Position.Y);
                layer.Texts.Add((formatted, transform));
            }

            result.Add(layer);
        }

        return result;
    }

    /// <summary>One geometry per line width; the pen width is in mm and scales with the zoom.</summary>
    private static IEnumerable<(Pen, StreamGeometry)> StrokeGeometries(IEnumerable<BoardGraphic> graphics, IBrush brush, double minWidth = 0.02)
    {
        foreach (var group in graphics.GroupBy(g => Math.Round(g.Width, 4)))
        {
            var geometry = new StreamGeometry();
            using (var g = geometry.Open())
            {
                foreach (var graphic in group)
                {
                    g.BeginFigure(new Point(graphic.Points[0].X, graphic.Points[0].Y), false);
                    for (var i = 1; i < graphic.Points.Count; i++)
                        g.LineTo(new Point(graphic.Points[i].X, graphic.Points[i].Y));
                    g.EndFigure(false);
                }
            }

            yield return (new Pen(brush, Math.Max(group.Key, minWidth), lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round), geometry);
        }
    }

    private static StreamGeometry? FillGeometry(IEnumerable<BoardGraphic> graphics)
    {
        var list = graphics.ToList();
        if (list.Count == 0)
            return null;

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.SetFillRule(FillRule.NonZero);
            foreach (var fill in list)
            {
                g.BeginFigure(new Point(fill.Points[0].X, fill.Points[0].Y), true);
                for (var i = 1; i < fill.Points.Count; i++)
                    g.LineTo(new Point(fill.Points[i].X, fill.Points[i].Y));
                g.EndFigure(true);
            }
        }

        return geometry;
    }

    /// <summary>The visible layers, the far side first and faint, so the viewed side lies on top.</summary>
    private void DrawLayers(DrawingContext context, Board board, Matrix view)
    {
        _layers ??= BuildLayers(board);
        if (VisibleLayers is not { Count: > 0 } visible)
            return;

        var far = FarSide;
        var ordered = _layers
            .Where(l => visible.Contains(l.Name))
            .OrderBy(l => l.Side == far ? 0 : l.Side is null ? 1 : 2)
            .ThenBy(l => l.Name.EndsWith(".Cu", StringComparison.Ordinal) ? 0 : 1);

        foreach (var layer in ordered)
        {
            var opacity = layer.Side == far ? 0.3 : layer.Name.EndsWith(".Cu", StringComparison.Ordinal) ? 0.75 : 0.9;
            using (context.PushOpacity(opacity))
            using (context.PushTransform(view))
            {
                if (layer.Fill is not null)
                    using (context.PushOpacity(0.45))
                        context.DrawGeometry(new SolidColorBrush(layer.Color), null, layer.Fill);

                foreach (var (pen, geometry) in layer.Strokes)
                    context.DrawGeometry(null, pen, geometry);

                foreach (var (text, transform) in layer.Texts)
                    using (context.PushTransform(transform))
                        context.DrawText(text, new Point(0, 0));
            }
        }
    }

    // ----- Highlighted nets -----

    /// <summary>
    /// Copper of a net per layer in board coordinates, built once when the net is highlighted first: one geometry per
    /// track width and one for the zones. Rebuilding it on every frame made large nets (e.g. GND) slow.
    /// </summary>
    private sealed class NetGeometry
    {
        public required List<NetLayer> Layers { get; init; }
        public required List<Pad> Pads { get; init; }
    }

    private sealed record NetLayer(BoardSide? Side, List<(double Width, StreamGeometry Geometry)> Strokes, StreamGeometry? Zones);

    private Dictionary<string, NetGeometry>? _nets;

    private NetGeometry NetGeometryOf(Board board, string net)
    {
        _nets ??= [];
        if (_nets.TryGetValue(net, out var cached))
            return cached;

        var geometry = new NetGeometry
        {
            Layers = board.GraphicsOfNet(net).GroupBy(g => g.Layer)
                .Select(g => new NetLayer(Board.SideOfLayer(g.Key), StrokeGeometriesByWidth(g.Where(x => !x.Filled)), FillGeometry(g.Where(x => x.Filled))))
                .ToList(),
            Pads = board.Pads.Where(p => p.Net == net).ToList()
        };
        _nets[net] = geometry;
        return geometry;
    }

    private static List<(double Width, StreamGeometry Geometry)> StrokeGeometriesByWidth(IEnumerable<BoardGraphic> graphics)
    {
        var result = new List<(double, StreamGeometry)>();
        foreach (var group in graphics.GroupBy(g => Math.Round(g.Width, 4)))
        {
            var geometry = new StreamGeometry();
            using (var g = geometry.Open())
            {
                foreach (var graphic in group)
                {
                    g.BeginFigure(new Point(graphic.Points[0].X, graphic.Points[0].Y), false);
                    for (var i = 1; i < graphic.Points.Count; i++)
                        g.LineTo(new Point(graphic.Points[i].X, graphic.Points[i].Y));
                    g.EndFigure(false);
                }
            }

            result.Add((group.Key, geometry));
        }

        return result;
    }

    /// <summary>
    /// Every highlighted net: tracks and vias in its color, the far side fainter, and its pads. Outline highlights are
    /// drawn first, so a net of the current step lies on top of a failed one; solid highlights in reverse order, so the
    /// first of the list (e.g. the H side of a measurement) lies on top. Zones only when enabled, then outlined and
    /// filled faintly: a ground pour would otherwise cover the whole board.
    /// </summary>
    /// <summary>
    /// Tracks and vias of a net, the far side fainter, and its zones when enabled (outlined and filled faintly). An
    /// outline highlight draws dashed center lines of the tracks instead and leaves the zones out.
    /// </summary>
    private void DrawNetCopper(DrawingContext context, NetGeometry geometry, IBrush brush, bool isOutline, Matrix view)
    {
        // Pen widths in board units for a width in pixels on screen
        var pixel = 1 / _scale;
        // Dashed center lines of the tracks, a few pixels wide on screen; the dashes scale with the pen
        var dashedPen = new Pen(brush, 2.5 * pixel, new DashStyle([2.5, 1.5], 0), PenLineCap.Round, PenLineJoin.Round);

        foreach (var layer in geometry.Layers.OrderBy(l => l.Side == FarSide ? 0 : 1))
        {
            var hasZones = IsZoneHighlightEnabled && layer.Zones is not null && !isOutline;
            if (layer.Strokes.Count == 0 && !hasZones)
                continue;

            using (context.PushOpacity(layer.Side == FarSide ? 0.45 : 0.95))
            using (context.PushTransform(view))
            {
                // Zones filled only, without outline: KiCad joins every hole of a filled zone to its edge with a
                // zero-width cut to the left, and an outline would draw each cut as a line
                if (hasZones)
                    using (context.PushOpacity(0.28))
                        context.DrawGeometry(brush, null, layer.Zones!);

                foreach (var (width, stroke) in layer.Strokes)
                    context.DrawGeometry(null, isOutline
                        ? dashedPen
                        : new Pen(brush, Math.Max(width, 2 * pixel), lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round), stroke);
            }
        }
    }

    private void DrawNetHighlights(DrawingContext context, Board board, Matrix view)
    {
        if (NetHighlights is not { Count: > 0 } highlights)
            return;

        var ordered = highlights.Where(h => h.Style == NetHighlightStyle.Outline)
            .Concat(highlights.Where(h => h.Style == NetHighlightStyle.Solid).Reverse());
        foreach (var highlight in ordered)
        {
            var geometry = NetGeometryOf(board, highlight.Net);
            var brush = new SolidColorBrush(highlight.Color);
            var isOutline = highlight.Style == NetHighlightStyle.Outline;
            DrawNetCopper(context, geometry, brush, isOutline, view);

            foreach (var pad in geometry.Pads)
            {
                if (isOutline)
                    DrawPadShape(context, pad, view, null, new Pen(brush, 2.5, new DashStyle([2.5, 1.5], 0)));
                else
                    DrawPadShape(context, pad, view, WithOpacity(brush, PadSideForDrawing(pad) == ViewSide ? 1 : 0.5), new Pen(brush, 1.5));
            }
        }
    }

    /// <summary>
    /// The overlay: shaded areas, thin guide lines and the outlines, dashed a few pixels wide in the overlay or foreground
    /// color, whatever the zoom; the highlighted line on top.
    /// </summary>
    private void DrawOverlayOutlines(DrawingContext context, Matrix view)
    {
        var brush = OverlayBrush ?? Foreground;

        static StreamGeometry Polyline(IReadOnlyList<Point2> points, Matrix view, bool filled)
        {
            var geometry = new StreamGeometry();
            using var g = geometry.Open();
            g.BeginFigure(ToScreenPoint(points[0], view), filled);
            for (var i = 1; i < points.Count; i++)
                g.LineTo(ToScreenPoint(points[i], view));
            g.EndFigure(filled);
            return geometry;
        }

        if (OverlayFills is { Count: > 0 } fills)
        {
            var fill = WithOpacity(brush, 0.25);
            foreach (var area in fills.Where(a => a.Count >= 3))
                context.DrawGeometry(fill, null, Polyline(area, view, true));
        }

        if (OverlayGuides is { Count: > 0 } guides)
        {
            var thin = new Pen(brush, 1);
            using (context.PushOpacity(0.6))
            {
                foreach (var guide in guides.Where(o => o.Count >= 2))
                    context.DrawGeometry(null, thin, Polyline(guide, view, false));
            }
        }

        if (OverlayOutlines is { Count: > 0 } outlines)
        {
            var pen = new Pen(brush, 1.5, new DashStyle([4, 3], 0), PenLineCap.Flat, PenLineJoin.Round);
            using (context.PushOpacity(0.85))
            {
                foreach (var outline in outlines.Where(o => o.Count >= 2))
                    context.DrawGeometry(null, pen, Polyline(outline, view, false));
            }
        }

        if (OverlayHighlight is { Count: >= 2 } highlight)
            context.DrawGeometry(null, new Pen(SelectionBrush ?? Foreground, 3, lineJoin: PenLineJoin.Round), Polyline(highlight, view, false));
    }

    /// <summary>Components outlined: their courtyard, or the area of their pads if they have none, and the reference.</summary>
    private void DrawFootprintHighlights(DrawingContext context, Board board, Matrix view)
    {
        if (FootprintHighlights is not { Count: > 0 } highlights)
            return;

        foreach (var highlight in highlights)
        {
            var footprint = board.Footprints.FirstOrDefault(f => f.Reference == highlight.Reference);
            if (footprint is null)
                continue;

            var brush = new SolidColorBrush(highlight.Color);
            var pen = new Pen(brush, 2.5);
            Point labelAt;
            if (footprint.HasCourtyard)
            {
                foreach (var (a, b) in footprint.CourtyardEdges)
                    context.DrawLine(pen, ToScreenPoint(a, view), ToScreenPoint(b, view));
                var bounds = footprint.CourtyardBounds;
                labelAt = ToScreenPoint(new Point2(bounds.MinX, bounds.MinY), view);
            }
            else
            {
                var bounds = Bounds2.Of(footprint.Pads.Select(p => p.Position));
                var corners = new[]
                {
                    new Point2(bounds.MinX - 0.5, bounds.MinY - 0.5), new Point2(bounds.MaxX + 0.5, bounds.MinY - 0.5),
                    new Point2(bounds.MaxX + 0.5, bounds.MaxY + 0.5), new Point2(bounds.MinX - 0.5, bounds.MaxY + 0.5)
                }.Select(c => ToScreenPoint(c, view)).ToArray();
                for (var i = 0; i < 4; i++)
                    context.DrawLine(pen, corners[i], corners[(i + 1) % 4]);
                labelAt = corners[0];
            }

            var label = Text(footprint.Reference, brush, 12, FontWeight.Bold);
            context.DrawText(label, new Point(labelAt.X, labelAt.Y - label.Height - 2));
        }
    }

    /// <summary>Single pads in a color with a label, e.g. the pins of a boundary scan step.</summary>
    private void DrawPadHighlights(DrawingContext context, Matrix view)
    {
        if (PadHighlights is not { Count: > 0 } highlights)
            return;

        var taken = new List<Rect>();
        foreach (var highlight in highlights)
        {
            var brush = new SolidColorBrush(highlight.Color);
            DrawPadShape(context, highlight.Pad, view, brush, new Pen(Foreground, 1));
            if (string.IsNullOrEmpty(highlight.Label))
                continue;

            var center = ToScreenPoint(highlight.Pad.Position, view);
            var label = Text(highlight.Label, brush, FontSize, FontWeight.SemiBold);
            DrawLabel(context, label, center, Math.Max(highlight.Pad.MinSize / 2 * _scale, 3), taken);
        }
    }

    /// <summary>The brush the user chose for pads or holes, else the one of the theme.</summary>
    private IBrush ChosenBrush(string layer, IBrush themeBrush) =>
        LayerColors is not null && LayerColors.TryGetValue(layer, out var color) ? new SolidColorBrush(color) : themeBrush;

    private IBrush TopPads => ChosenBrush(Controls.LayerColors.TopPads, TopPadBrush);
    private IBrush BottomPads => ChosenBrush(Controls.LayerColors.BottomPads, BottomPadBrush);

    private void DrawHoles(DrawingContext context, Board board, Matrix view)
    {
        var pen = new Pen(ChosenBrush(Controls.LayerColors.Holes, MutedBrush), 1.2, new DashStyle([3, 2], 0));
        foreach (var hole in board.Holes)
        {
            var r = hole.Diameter / 2 * _scale;
            context.DrawEllipse(null, pen, ToScreenPoint(hole.Position, view), r, r);
        }
    }

    private void DrawPads(DrawingContext context, Board board, Matrix view)
    {
        var highlighted = HighlightedPads;
        var selectedNet = SelectedPad is { HasNet: true } s ? s.Net : null;

        // The far side first, so pads of the viewed side lie on top. Each side's pads can be hidden on their own; a
        // pad with copper on both sides shows while either of them is shown
        var showTop = IsLayerVisible(Controls.LayerColors.TopPads);
        var showBottom = IsLayerVisible(Controls.LayerColors.BottomPads);
        foreach (var pass in new[] { FarSide, ViewSide })
        {
            foreach (var pad in board.Pads)
            {
                if (PadSideForDrawing(pad) != pass || !(pad.HasTopCopper && showTop || pad.HasBottomCopper && showBottom))
                    continue;

                var brush = pass == BoardSide.Top ? TopPads : BottomPads;
                var opacity = pass == ViewSide ? 0.9 : 0.25;
                if (highlighted is not null && !highlighted.Contains(pad))
                    opacity *= 0.3;
                if (selectedNet is not null && pad.Net == selectedNet)
                    opacity = 1;

                DrawPadShape(context, pad, view, WithOpacity(brush, opacity), null);
            }
        }
    }

    /// <summary>The selected pad and the pads of its net, and the hovered pad, outlined on top of everything.</summary>
    private void DrawSelection(DrawingContext context, Board board, Matrix view)
    {
        if (SelectedPad is { } selected)
        {
            var selection = SelectionBrush ?? Foreground;

            // The whole net of the clicked pad: tracks, vias (and zones when enabled) and the other pads
            if (selected.HasNet)
            {
                var geometry = NetGeometryOf(board, selected.Net);
                DrawNetCopper(context, geometry, selection, false, view);

                var netPen = new Pen(selection, 1.5);
                foreach (var pad in geometry.Pads.Where(p => p != selected))
                    DrawPadShape(context, pad, view, WithOpacity(selection, PadSideForDrawing(pad) == ViewSide ? 0.6 : 0.3), netPen);
            }

            DrawPadShape(context, selected, view, null, new Pen(selection, 2.5));
        }

        if (_hoveredPad is { } hovered && hovered != SelectedPad)
            DrawPadShape(context, hovered, view, null, new Pen(Foreground, 1.5));
    }

    /// <summary>Through-hole pads are drawn with the viewed side, SMD pads with their own side.</summary>
    private BoardSide PadSideForDrawing(Pad pad) =>
        pad.Kind == PadKind.ThroughHole ? ViewSide : pad.HasTopCopper ? BoardSide.Top : BoardSide.Bottom;

    private void DrawPadShape(DrawingContext context, Pad pad, Matrix view, IBrush? fill, Pen? pen)
    {
        var local = Matrix.CreateRotation(-pad.Rotation * Math.PI / 180) * Matrix.CreateTranslation(pad.Position.X, pad.Position.Y);

        // A pen inside the transform would scale with the zoom; outline the pad on screen instead
        var rect = new Rect(-pad.Width / 2, -pad.Height / 2, pad.Width, pad.Height);
        var radius = pad.Shape switch
        {
            PadShape.Circle or PadShape.Oval => Math.Min(pad.Width, pad.Height) / 2,
            PadShape.RoundRect => Math.Min(pad.Width, pad.Height) * 0.25,
            _ => 0
        };

        var minPixels = Math.Min(pad.Width, pad.Height) * _scale;
        if (fill is not null)
        {
            using (context.PushTransform(local * view))
            {
                if (pad.Shape == PadShape.Circle)
                    context.DrawEllipse(fill, null, rect.Center, rect.Width / 2, rect.Height / 2);
                else
                    context.DrawRectangle(fill, null, rect, radius, radius);
            }

            // Tiny pads still get a visible dot when zoomed out
            if (minPixels < 2)
                context.DrawEllipse(fill, null, ToScreenPoint(pad.Position, view), 1, 1);
        }

        if (pen is null)
            return;

        if (pad.Shape is PadShape.Circle && Math.Abs(pad.Width - pad.Height) < 1e-6)
        {
            var r = Math.Max(pad.Width / 2 * _scale, 2) + pen.Thickness / 2;
            context.DrawEllipse(null, pen, ToScreenPoint(pad.Position, view), r, r);
            return;
        }

        var corners = new[] { rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft }
            .Select(c => c.Transform(local * view)).ToList();
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(corners[0], false);
            for (var i = 1; i < corners.Count; i++)
                g.LineTo(corners[i]);
            g.EndFigure(true);
        }

        context.DrawGeometry(null, pen, geometry);
    }

    // ----- Markers -----

    private IBrush BrushOf(IBoardMarker marker) =>
        marker.MarkerColor is { } color
            ? new SolidColorBrush(color)
            : marker.MarkerState switch
            {
                BoardMarkerState.Error => ErrorBrush,
                BoardMarkerState.Warning => WarningBrush,
                _ => ProbeBrush
            };

    /// <summary>
    /// Tooling pins (ring with a plus), pre-centering pins (filled disc), support pins (ring with a dot) and push rods (ring
    /// with a cross), at their real size
    /// but at least a few pixels; dashed when they are on the far side of the view. The selected one has a ring around.
    /// </summary>
    private void DrawPartMarkers(DrawingContext context, Matrix view)
    {
        if (PartMarkers is not { Count: > 0 } markers)
            return;

        foreach (var marker in markers)
        {
            if (marker.MarkerPosition is not { } position)
                continue;

            IBrush brush = marker.MarkerColor is { } color
                ? new SolidColorBrush(color)
                : PartBrush(marker.MarkerKind);
            if (marker.MarkerState == BoardMarkerState.Error)
                brush = ErrorBrush;
            else if (marker.MarkerState == BoardMarkerState.Warning)
                brush = WarningBrush;

            var center = ToScreenPoint(position, view);
            var r = Math.Max(marker.MarkerSize / 2 * _scale, 5);
            var far = marker.MarkerSide is { } side && side != ViewSide;
            var pen = far ? new Pen(brush, 2, new DashStyle([2, 2], 0)) : new Pen(brush, 2.5);
            var thin = new Pen(brush, 1.5);

            switch (marker.MarkerKind)
            {
                case BoardMarkerKind.ToolingPin:
                    context.DrawEllipse(WithOpacity(brush, 0.25), new Pen(brush, 2.5), center, r, r);
                    context.DrawLine(thin, new Point(center.X - r * 0.6, center.Y), new Point(center.X + r * 0.6, center.Y));
                    context.DrawLine(thin, new Point(center.X, center.Y - r * 0.6), new Point(center.X, center.Y + r * 0.6));
                    break;
                case BoardMarkerKind.PreCenteringPin:
                    context.DrawEllipse(far ? null : WithOpacity(brush, 0.45), pen, center, r, r);
                    break;
                case BoardMarkerKind.SupportPin:
                    context.DrawEllipse(far ? null : WithOpacity(brush, 0.2), pen, center, r, r);
                    context.DrawEllipse(brush, null, center, Math.Max(r * 0.25, 1.5), Math.Max(r * 0.25, 1.5));
                    break;
                default:
                    context.DrawEllipse(far ? null : WithOpacity(brush, 0.2), pen, center, r, r);
                    var d = r * 0.7;
                    context.DrawLine(thin, new Point(center.X - d, center.Y - d), new Point(center.X + d, center.Y + d));
                    context.DrawLine(thin, new Point(center.X - d, center.Y + d), new Point(center.X + d, center.Y - d));
                    break;
            }

            if (ReferenceEquals(marker, SelectedPartMarker))
                context.DrawEllipse(null, new Pen(SelectionBrush ?? Foreground, 2), center, r + 4, r + 4);

            var label = Text(marker.MarkerLabel, brush, FontSize, FontWeight.SemiBold);
            context.DrawText(label, new Point(center.X + r + 3, center.Y - label.Height / 2));
        }
    }

    private IBrush PartBrush(BoardMarkerKind kind) => kind switch
    {
        BoardMarkerKind.ToolingPin => ToolingPinBrush,
        BoardMarkerKind.PreCenteringPin => PreCenteringPinBrush,
        BoardMarkerKind.SupportPin => SupportPinBrush,
        _ => PushRodBrush
    };

    /// <summary>The part marker under a point of the control, the nearest if several overlap.</summary>
    private IBoardMarker? PartMarkerAt(Point screen)
    {
        if (Board is not { } board || PartMarkers is not { Count: > 0 } markers)
            return null;

        var view = ViewMatrix(board);
        return markers
            .Where(m => m.MarkerPosition is not null)
            .Select(m => (Marker: m, Distance: Distance(ToScreenPoint(m.MarkerPosition!.Value, view), screen),
                Radius: Math.Max(m.MarkerSize / 2 * _scale, 5) + 3))
            .Where(m => m.Distance <= m.Radius)
            .OrderBy(m => m.Distance)
            .Select(m => m.Marker)
            .FirstOrDefault();

        static double Distance(Point a, Point b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }

    private void DrawMarkers(DrawingContext context, Matrix view)
    {
        if (Markers is not { Count: > 0 } all)
            return;

        var markers = all
            .Where(m => m.MarkerPosition is not null)
            .Select(m => (Marker: m, Center: ToScreenPoint(m.MarkerPosition!.Value, view), Radius: Math.Clamp(m.MarkerSize / 2 * _scale * 0.7, 4, 14)))
            .ToList();

        foreach (var (marker, center, radius) in markers)
        {
            var brush = BrushOf(marker);
            var isViewedSide = marker.MarkerSide is null || marker.MarkerSide == ViewSide;
            if (isViewedSide)
                context.DrawEllipse(WithOpacity(brush, 0.85), new Pen(Foreground, marker == SelectedMarker ? 2.5 : 1), center, radius, radius);
            else
                // From the other side: hollow, dashed
                context.DrawEllipse(null, new Pen(WithOpacity(brush, 0.8), 1.5, new DashStyle([3, 2], 0)), center, radius, radius);

            if (marker == SelectedMarker)
                context.DrawEllipse(null, new Pen(AccentBrush, 2), center, radius + 4, radius + 4);
        }

        // Labels at the first of several places around the marker that is still free
        var taken = markers.Select(m => new Rect(m.Center.X - m.Radius, m.Center.Y - m.Radius, 2 * m.Radius, 2 * m.Radius)).ToList();
        foreach (var (marker, center, radius) in markers)
        {
            var isViewedSide = marker.MarkerSide is null || marker.MarkerSide == ViewSide;
            var label = Text(marker.MarkerLabel, isViewedSide ? Foreground : MutedBrush, FontSize, FontWeight.SemiBold);
            DrawLabel(context, label, center, radius, taken);
        }
    }

    /// <summary>Text next to a point, at the first of eight places that overlaps no other label or marker.</summary>
    private static void DrawLabel(DrawingContext context, FormattedText label, Point center, double radius, List<Rect> taken)
    {
        var gap = radius + 2;
        var places = new[]
        {
            new Point(center.X + gap, center.Y - label.Height / 2),
            new Point(center.X - gap - label.Width, center.Y - label.Height / 2),
            new Point(center.X - label.Width / 2, center.Y - gap - label.Height),
            new Point(center.X - label.Width / 2, center.Y + gap),
            new Point(center.X + gap, center.Y - gap - label.Height + 4),
            new Point(center.X - gap - label.Width, center.Y - gap - label.Height + 4),
            new Point(center.X + gap, center.Y + gap - 4),
            new Point(center.X - gap - label.Width, center.Y + gap - 4)
        };

        var place = places.Select(p => new Rect(p, new Size(label.Width, label.Height)))
            .FirstOrDefault(r => !taken.Any(t => t.Intersects(r)), new Rect(places[0], new Size(label.Width, label.Height)));
        taken.Add(place);
        context.DrawText(label, place.TopLeft);
    }

    // ----- Texts -----

    private void DrawLegend(DrawingContext context)
    {
        var side = Text(BoardTexts.Get(ViewSide == BoardSide.Top ? "Board_TopView" : "Board_BottomView"), Foreground, 12, FontWeight.SemiBold);
        context.DrawText(side, new Point(Bounds.Width - side.Width - 10, 8));

        // Pad colors under the side, right aligned
        var x = Bounds.Width - 10;
        var y = 8 + side.Height + 4;
        foreach (var (brush, key) in new[] { (BottomPads, "Board_BottomPads"), (TopPads, "Board_TopPads") })
        {
            var text = Text(BoardTexts.Get(key), MutedBrush, FontSize);
            x -= text.Width;
            context.DrawText(text, new Point(x, y));
            x -= 14;
            context.DrawRectangle(brush, null, new Rect(x, y + 3, 10, 10), 2, 2);
            x -= 14;
        }

        if (ShowMarkerLegend)
            DrawMarkerLegend(context, y + 18);

        if (string.IsNullOrEmpty(HintKey))
            return;

        var hint = Text(BoardTexts.Get(HintKey), MutedBrush, FontSize);
        hint.MaxTextWidth = Math.Max(10, Bounds.Width - 20);
        hint.Trimming = TextTrimming.CharacterEllipsis;
        context.DrawText(hint, new Point(10, Bounds.Height - hint.Height - 8));
    }

    /// <summary>
    /// The kinds of markers on the board, right aligned in one line: each with the symbol it has on the board, so the
    /// colors can be told apart.
    /// </summary>
    private void DrawMarkerLegend(DrawingContext context, double y)
    {
        var kinds = new List<(string Key, Action<Point> Symbol)>();
        var parts = PartMarkers ?? [];

        if (Markers is { Count: > 0 })
            kinds.Add(("Board_LegendProbes", c => context.DrawEllipse(ProbeBrush, null, c, 5, 5)));
        if (parts.Any(m => m.MarkerKind == BoardMarkerKind.ToolingPin))
            kinds.Add(("Board_LegendToolingPins", c =>
            {
                context.DrawEllipse(WithOpacity(ToolingPinBrush, 0.25), new Pen(ToolingPinBrush, 1.5), c, 5, 5);
                context.DrawLine(new Pen(ToolingPinBrush, 1), new Point(c.X - 3, c.Y), new Point(c.X + 3, c.Y));
                context.DrawLine(new Pen(ToolingPinBrush, 1), new Point(c.X, c.Y - 3), new Point(c.X, c.Y + 3));
            }));
        if (parts.Any(m => m.MarkerKind == BoardMarkerKind.PreCenteringPin))
            kinds.Add(("Board_LegendPreCenteringPins", c => context.DrawEllipse(WithOpacity(PreCenteringPinBrush, 0.45), new Pen(PreCenteringPinBrush, 1.5), c, 5, 5)));
        if (parts.Any(m => m.MarkerKind == BoardMarkerKind.SupportPin))
            kinds.Add(("Board_LegendSupportPins", c =>
            {
                context.DrawEllipse(WithOpacity(SupportPinBrush, 0.2), new Pen(SupportPinBrush, 1.5), c, 5, 5);
                context.DrawEllipse(SupportPinBrush, null, c, 1.5, 1.5);
            }));
        if (parts.Any(m => m.MarkerKind == BoardMarkerKind.PushRod))
            kinds.Add(("Board_LegendPushRods", c =>
            {
                context.DrawEllipse(WithOpacity(PushRodBrush, 0.2), new Pen(PushRodBrush, 1.5), c, 5, 5);
                context.DrawLine(new Pen(PushRodBrush, 1), new Point(c.X - 3, c.Y - 3), new Point(c.X + 3, c.Y + 3));
                context.DrawLine(new Pen(PushRodBrush, 1), new Point(c.X - 3, c.Y + 3), new Point(c.X + 3, c.Y - 3));
            }));
        if (OverlayOutlines is { Count: > 0 })
            kinds.Add(("Board_LegendPlateOutline", c =>
                context.DrawLine(new Pen(OverlayBrush ?? Foreground, 1.5, new DashStyle([3, 2], 0)), new Point(c.X - 6, c.Y), new Point(c.X + 6, c.Y))));

        // From the right: text, then its symbol before it
        var x = Bounds.Width - 10;
        foreach (var (key, symbol) in Enumerable.Reverse(kinds))
        {
            var text = Text(BoardTexts.Get(key), MutedBrush, FontSize);
            x -= text.Width;
            context.DrawText(text, new Point(x, y));
            symbol(new Point(x - 10, y + text.Height / 2));
            x -= 26;
        }
    }

    /// <summary>The tooling pin standing in the hole at this point of the board, if any.</summary>
    private IBoardMarker? ToolingPinAt(Point2 position) =>
        PartMarkers?.FirstOrDefault(m => m.MarkerKind == BoardMarkerKind.ToolingPin &&
                                         m.MarkerPosition is { } p && p.DistanceTo(position) < 1e-3);

    private void DrawHoverInfo(DrawingContext context)
    {
        if (_hoveredPad is null && _hoveredHole is { } hole)
        {
            var tooling = ToolingPinAt(hole.Position);
            var holeInfo = BoardTexts.Format(tooling is null ? "Board_HoleInfo" : "Board_ToolingPinInfo", hole.FootprintReference,
                hole.Diameter.ToString("0.##", CultureInfo.CurrentCulture), tooling?.MarkerLabel ?? string.Empty);
            context.DrawText(Text(holeInfo, Foreground, 12, FontWeight.SemiBold), new Point(10, 8));
            return;
        }

        if (_hoveredPad is not { } pad)
            return;

        var kind = pad.Kind == PadKind.Smd ? "SMD" : "THT";
        var net = pad.HasNet ? BoardTexts.ShortNet(pad.Net) : BoardTexts.Get("Board_NoNet");
        var info = string.Create(CultureInfo.CurrentCulture,
            $"{pad.Footprint.Reference}.{pad.Number}  ·  {net}  ·  {kind} {pad.Width:0.##}×{pad.Height:0.##} mm");
        var markers = Markers?
            .Where(m => m.MarkerPad == pad)
            .Select(m => m.MarkerSide is { } s
                ? $"{m.MarkerLabel} ({BoardTexts.Get(s == BoardSide.Top ? "Board_SideTop" : "Board_SideBottom")})"
                : m.MarkerLabel)
            .ToList();
        if (ToolingPinAt(pad.Position) is { } pin)
            (markers ??= []).Add(pin.MarkerLabel);
        if (markers is { Count: > 0 })
            info += "  ·  " + string.Join(", ", markers);

        context.DrawText(Text(info, Foreground, 12, FontWeight.SemiBold), new Point(10, 8));
    }

    private static FormattedText Text(string text, IBrush brush, double size, FontWeight weight = FontWeight.Normal) =>
        new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(Typeface.FontFamily, FontStyle.Normal, weight), size, brush);

    private static IBrush WithOpacity(IBrush brush, double opacity) =>
        brush is ISolidColorBrush solid ? new SolidColorBrush(solid.Color, opacity * solid.Opacity) : brush;

    // ----- Mouse -----

    /// <summary>Pad under the pointer; pads of the viewed side win over the far side.</summary>
    private Pad? PadAt(Point screen)
    {
        if (Board is not { } board)
            return null;

        var p = ToBoard(board, screen);
        var tolerance = 3 / _scale; // a few pixels around tiny pads
        Pad? best = null;
        var bestScore = double.MaxValue;

        foreach (var pad in board.Pads)
        {
            // Into the pad's own unrotated coordinates
            var local = (p - pad.Position).Rotate(-pad.Rotation);
            var dx = Math.Abs(local.X) - pad.Width / 2;
            var dy = Math.Abs(local.Y) - pad.Height / 2;
            if (dx > tolerance || dy > tolerance)
                continue;

            var score = Math.Max(dx, dy) + (PadSideForDrawing(pad) == ViewSide ? 0 : 1000);
            if (score < bestScore)
            {
                bestScore = score;
                best = pad;
            }
        }

        return best;
    }

    /// <summary>Unplated hole under the pointer.</summary>
    private Hole? HoleAt(Point screen)
    {
        if (Board is not { } board || !IsLayerVisible(Controls.LayerColors.Holes))
            return null;

        var p = ToBoard(board, screen);
        var tolerance = 3 / _scale;
        return board.Holes
            .Where(h => h.Position.DistanceTo(p) <= h.Diameter / 2 + tolerance)
            .MinBy(h => h.Position.DistanceTo(p));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        var point = e.GetCurrentPoint(this);

        if (point.Properties.IsLeftButtonPressed && e.ClickCount == 2)
        {
            if (PadAt(point.Position) is { } pad)
            {
                if (PadActivatedCommand?.CanExecute(pad) == true)
                    PadActivatedCommand.Execute(pad);
            }
            else if (HoleAt(point.Position) is null)
            {
                FitToView();
            }

            e.Handled = true;
            return;
        }

        // A part marker under the left button is selected and can be dragged
        if (point.Properties.IsLeftButtonPressed && PartMarkerAt(point.Position) is { } part)
        {
            SelectedPartMarker = part;
            _draggedPart = part as IMovableBoardMarker;
            _dragStart = point.Position;
            _isDragging = false;
            e.Pointer.Capture(this);
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (point.Properties.IsLeftButtonPressed || point.Properties.IsMiddleButtonPressed || point.Properties.IsRightButtonPressed)
        {
            _dragStart = point.Position;
            _panAtDragStart = _pan;
            _isDragging = false;
            e.Pointer.Capture(this);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var position = e.GetPosition(this);

        if (_dragStart is { } start)
        {
            var delta = position - start;
            if (!_isDragging && Math.Abs(delta.X) + Math.Abs(delta.Y) > 4)
                _isDragging = true;

            if (_isDragging && _draggedPart is not null && Board is { } board)
            {
                _draggedPart.MoveTo(ToBoard(board, position));
                InvalidateVisual();
                return;
            }

            if (_isDragging)
            {
                _pan = _panAtDragStart + delta;
                _isUserView = true;
                InvalidateVisual();
                return;
            }
        }

        var hovered = PadAt(position);
        var hoveredHole = hovered is null ? HoleAt(position) : null;
        if (hovered != _hoveredPad || hoveredHole != _hoveredHole)
        {
            _hoveredPad = hovered;
            _hoveredHole = hoveredHole;
            Cursor = hovered is null && hoveredHole is null ? Cursor.Default : new Cursor(StandardCursorType.Hand);
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_dragStart is null)
            return;

        var wasDragging = _isDragging;
        var draggedPart = _draggedPart;
        _dragStart = null;
        _isDragging = false;
        _draggedPart = null;
        e.Pointer.Capture(null);

        if (draggedPart is not null)
        {
            if (wasDragging && PartMovedCommand?.CanExecute(draggedPart) == true)
                PartMovedCommand.Execute(draggedPart);
            return;
        }

        // In place mode a click places a part at the point
        if (!wasDragging && e.InitialPressMouseButton == MouseButton.Left && IsPlacementActive && Board is { } board)
        {
            var at = ToBoard(board, e.GetPosition(this));
            if (PlaceCommand?.CanExecute(at) == true)
                PlaceCommand.Execute(at);
            return;
        }

        if (!wasDragging && e.InitialPressMouseButton == MouseButton.Left)
            SelectedPartMarker = null;

        // A click without dragging selects the pad under the pointer (or clears the selection)
        if (!wasDragging && e.InitialPressMouseButton == MouseButton.Left)
            SelectedPad = PadAt(e.GetPosition(this));
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_hoveredPad is not null || _hoveredHole is not null)
        {
            _hoveredPad = null;
            _hoveredHole = null;
            InvalidateVisual();
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (Board is not { } board || e.Delta.Y == 0)
            return;

        var fit = FitScale(ViewArea(board));
        var factor = e.Delta.Y > 0 ? 1.25 : 0.8;
        var newScale = Math.Clamp(_scale * factor, fit * 0.5, fit * 200);
        if (Math.Abs(newScale - _scale) < 1e-12)
            return;

        // Keep the board point under the pointer where it is
        var pointer = e.GetPosition(this);
        var anchor = ToBoard(board, pointer);
        _scale = newScale;
        _isUserView = true;
        var moved = ToScreen(board, anchor);
        _pan += pointer - moved;

        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.F && e.KeyModifiers == KeyModifiers.None)
        {
            FitToView();
            e.Handled = true;
        }
    }
}