using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATLab.FixtureDesigner.Localization;
using ATLab.FixtureDesigner.Fixture;
using ATLab.FixtureDesigner.Plates;
using ATLab.FixtureDesigner.Reporting;
using ATLab.Pcb.Controls;
using ATLab.Pcb.Fixture;
using ATLab.Pcb.Kicad;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.FixtureDesigner.ViewModels;

/// <summary>A way to define the plate outline, as offered in the Manufacturing tab.</summary>
public sealed record OutlineModeOption(PlateOutlineMode Mode, string LabelKey)
{
    public string Label => Localizer.Get(LabelKey);
}

/// <summary>
/// The outlines of the plates: the board outline plus the margin, a fixed size, an outline imported from a DXF or SVG
/// file, or one drawn on a layer of the board. A fixed size or an imported outline is centered on the board and moved
/// by the offset. The plates share one outline, and any plate can have its own instead: the properties below are those
/// of the outline being edited, the others are kept aside until the user switches to them.
/// </summary>
public partial class MainWindowViewModel
{
    public IReadOnlyList<OutlineModeOption> OutlineModes { get; } =
    [
        new(PlateOutlineMode.Automatic, "Outline_Automatic"),
        new(PlateOutlineMode.FixedSize, "Outline_FixedSize"),
        new(PlateOutlineMode.Dxf, "Outline_Dxf"),
        new(PlateOutlineMode.Svg, "Outline_Svg"),
        new(PlateOutlineMode.BoardLayer, "Outline_BoardLayer")
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedOutlineMode), nameof(IsOutlineAutomatic), nameof(IsOutlineFixedSize),
        nameof(IsOutlineImported), nameof(IsOutlineBoardLayer), nameof(HasOutlineOffset), nameof(HasOutlineShapes))]
    private PlateOutlineMode _outlineMode;

    public OutlineModeOption SelectedOutlineMode
    {
        get => OutlineModes.First(m => m.Mode == OutlineMode);
        set
        {
            if (value is not null)
                OutlineMode = value.Mode;
        }
    }

    public bool IsOutlineAutomatic => OutlineMode == PlateOutlineMode.Automatic;
    public bool IsOutlineFixedSize => OutlineMode == PlateOutlineMode.FixedSize;
    public bool IsOutlineImported => OutlineMode is PlateOutlineMode.Dxf or PlateOutlineMode.Svg;
    public bool IsOutlineBoardLayer => OutlineMode == PlateOutlineMode.BoardLayer;
    public bool HasOutlineOffset => IsOutlineFixedSize || IsOutlineImported;

    /// <summary>Distance of the automatic outline from the board outline, mm.</summary>
    [ObservableProperty] private double _plateMargin = FixtureFile.DefaultPlateMargin;

    /// <summary>Size of the fixed-size outline, mm.</summary>
    [ObservableProperty] private double _outlineWidth = 200;
    [ObservableProperty] private double _outlineHeight = 150;

    /// <summary>Shift of the fixed-size or imported outline from the centered position, mm (X right, Y up).</summary>
    [ObservableProperty] private double _outlineOffsetX;
    [ObservableProperty] private double _outlineOffsetY;

    /// <summary>The DXF or SVG file the outline was read from.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutlineFileName))]
    private string? _outlineFile;

    public string OutlineFileName => OutlineFile is null ? string.Empty : Path.GetFileName(OutlineFile);

    /// <summary>Layer of the board with the outline.</summary>
    [ObservableProperty] private string? _outlineLayer;

    /// <summary>The outline read from the file (kept in the fixture, so it works without the file).</summary>
    private OutlineShape? _importedOutline;
    private PlateOutlineMode _importedMode;

    /// <summary>Layers of the board an outline can be drawn on.</summary>
    public IReadOnlyList<string> OutlineLayerNames => Board is null ? [] : PlateOutlines.OutlineLayers(Board);

    /// <summary>Size and frame holes of the outline, or why it is missing, and what of the drawing was not used.</summary>
    [ObservableProperty] private string _outlineStatus = string.Empty;

    [ObservableProperty] private bool _isOutlineMissing;

    /// <summary>The outlines of the plates, drawn over the board so their position can be checked.</summary>
    [ObservableProperty] private IReadOnlyList<BoardOverlayOutline>? _outlineOverlay;

    /// <summary>The shape chosen in the list, in board coordinates.</summary>
    [ObservableProperty] private IReadOnlyList<Point2>? _outlineHighlight;

    // ----- Shapes of an imported outline: through or pockets -----

    /// <summary>The circles and cut-outs of the imported outline being edited.</summary>
    public System.Collections.ObjectModel.ObservableCollection<OutlineShapeRow> OutlineShapes { get; } = [];

    public bool HasOutlineShapes => IsOutlineImported && OutlineShapes.Count > 0;

    [ObservableProperty] private OutlineShapeRow? _selectedOutlineShape;

    /// <summary>The imported outline the shape rows show.</summary>
    private OutlineShape? _shapeRowsSource;

    partial void OnSelectedOutlineShapeChanged(OutlineShapeRow? value) => UpdateOutlineState();

    /// <summary>New rows when another imported outline is edited; a change of the rows' own keeps them.</summary>
    private void SyncShapeRows()
    {
        var shape = IsOutlineImported ? _importedOutline : null;
        if (ReferenceEquals(shape, _shapeRowsSource))
            return;

        _shapeRowsSource = shape;
        foreach (var row in OutlineShapes)
            row.PropertyChanged -= OnShapeRowChanged;
        SelectedOutlineShape = null;
        OutlineShapes.Clear();

        if (shape is not null)
        {
            var culture = CultureInfo.CurrentCulture;
            OutlineShapeRow Row(bool circle, int index, string name, Pocket? pocket)
            {
                var row = new OutlineShapeRow(circle, index, name);
                if (pocket is { } p)
                {
                    row.IsPocket = true;
                    row.Depth = p.Depth;
                    row.Side = PocketSideOption.All.First(o => o.Side == p.Side);
                }

                row.PropertyChanged += OnShapeRowChanged;
                return row;
            }

            // Circles named like the frame holes they become (M1, M2, …)
            for (var i = 0; i < shape.Circles.Count; i++)
            {
                var c = shape.Circles[i];
                OutlineShapes.Add(Row(true, i, Localizer.Format("Shape_Circle", i + 1, c.Diameter.ToString("0.0#", culture)), c.Pocket));
            }

            for (var i = 0; i < shape.Cutouts.Count; i++)
                OutlineShapes.Add(Row(false, i, Localizer.Format("Shape_Cutout", i + 1), shape.Cutouts[i].Pocket));
        }

        OnPropertyChanged(nameof(HasOutlineShapes));
    }

    private void OnShapeRowChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(OutlineShapeRow.IsPocket) or nameof(OutlineShapeRow.Depth) or nameof(OutlineShapeRow.Side)) ||
            _importedOutline is null)
            return;

        Pocket? PocketOf(bool circle, int index) => OutlineShapes.FirstOrDefault(r => r.IsCircle == circle && r.Index == index)?.Pocket;
        _importedOutline = _importedOutline.WithPockets(i => PocketOf(true, i), i => PocketOf(false, i));
        _shapeRowsSource = _importedOutline;
        OnOutlineChanged();
    }

    // ----- One outline for all plates, and the plates' own -----

    /// <summary>The outlines that can be edited: the shared one and those of the plates with their own.</summary>
    public System.Collections.ObjectModel.ObservableCollection<OutlineTarget> OutlineTargets { get; } = [new(null)];

    /// <summary>The outline the properties above show.</summary>
    [ObservableProperty] private OutlineTarget? _selectedOutlineTarget;

    /// <summary>Several outlines to choose from, so the choice is shown.</summary>
    public bool HasOwnOutlines => OutlineTargets.Count > 1;

    /// <summary>The settings of an outline, as kept for the outlines not being edited.</summary>
    private sealed class OutlineState
    {
        public PlateOutlineMode Mode { get; init; }
        public double Margin { get; init; } = FixtureFile.DefaultPlateMargin;
        public double Width { get; init; } = 200;
        public double Height { get; init; } = 150;
        public double OffsetX { get; init; }
        public double OffsetY { get; init; }
        public string? File { get; init; }
        public string? Layer { get; init; }
        public OutlineShape? Imported { get; init; }
        public PlateOutlineMode ImportedMode { get; init; }
    }

    /// <summary>The outline being edited: null for the shared one, else the plate whose own outline it is.</summary>
    private FixturePlate? _editedOutline;

    /// <summary>The shared outline while a plate's own is being edited.</summary>
    private OutlineState _sharedOutline = new();

    /// <summary>The plates with an outline of their own; the one being edited is in the properties instead.</summary>
    private readonly Dictionary<FixturePlate, OutlineState> _ownOutlines = [];

    /// <summary>True while the properties are filled from a kept outline: no change of the user.</summary>
    private bool _isApplyingOutline;

    private OutlineState CaptureOutline() => new()
    {
        Mode = OutlineMode,
        Margin = PlateMargin,
        Width = OutlineWidth,
        Height = OutlineHeight,
        OffsetX = OutlineOffsetX,
        OffsetY = OutlineOffsetY,
        File = OutlineFile,
        Layer = OutlineLayer,
        Imported = _importedOutline,
        ImportedMode = _importedMode
    };

    private void ApplyOutline(OutlineState state)
    {
        _isApplyingOutline = true;
        try
        {
            _importedOutline = state.Imported;
            _importedMode = state.ImportedMode;
            OutlineFile = state.File;
            OutlineLayer = state.Layer;
            PlateMargin = state.Margin;
            OutlineWidth = state.Width;
            OutlineHeight = state.Height;
            OutlineOffsetX = state.OffsetX;
            OutlineOffsetY = state.OffsetY;
            OutlineMode = state.Mode;
        }
        finally
        {
            _isApplyingOutline = false;
        }
    }

    /// <summary>An outline's settings: the edited one from the properties, the others as kept aside.</summary>
    private OutlineState StateOf(FixturePlate? key) =>
        key == _editedOutline ? CaptureOutline() : key is { } plate ? _ownOutlines[plate] : _sharedOutline;

    /// <summary>The outline settings a plate gets: its own, or the shared ones.</summary>
    private OutlineState OutlineOf(FixturePlate plate) => StateOf(_ownOutlines.ContainsKey(plate) ? plate : null);

    /// <summary>The edited outline is kept aside and the chosen one comes into the properties.</summary>
    private void EditOutline(FixturePlate? key)
    {
        if (key != _editedOutline)
        {
            var edited = CaptureOutline();
            var next = StateOf(key);
            if (_editedOutline is { } plate)
            {
                if (_ownOutlines.ContainsKey(plate))
                    _ownOutlines[plate] = edited;
            }
            else
            {
                _sharedOutline = edited;
            }

            _editedOutline = key;
            ApplyOutline(next);
        }

        var target = OutlineTargets.FirstOrDefault(t => t.Plate == key);
        if (SelectedOutlineTarget != target)
            SelectedOutlineTarget = target;
        RunChecks();
    }

    partial void OnSelectedOutlineTargetChanged(OutlineTarget? value)
    {
        if (value is not null && !_isApplyingOutline)
            EditOutline(value.Plate);
    }

    /// <summary>
    /// Switched on, the plate starts with the shared outline and it is edited; switched off, the plate has the shared
    /// outline again.
    /// </summary>
    private void OnOwnOutlineChanged(PlateRow row)
    {
        if (_isApplyingOutline || row.HasOwnOutline == _ownOutlines.ContainsKey(row.Plate))
            return;

        if (row.HasOwnOutline)
        {
            _ownOutlines[row.Plate] = StateOf(null);
            OutlineTargets.Add(new OutlineTarget(row.Plate));
            SortOutlineTargets();
            EditOutline(row.Plate);
        }
        else
        {
            if (_editedOutline == row.Plate)
                EditOutline(null);
            _ownOutlines.Remove(row.Plate);
            if (OutlineTargets.FirstOrDefault(t => t.Plate == row.Plate) is { } target)
                OutlineTargets.Remove(target);
        }

        OnPropertyChanged(nameof(HasOwnOutlines));
        MarkDirty();
        RunChecks();
    }

    /// <summary>The plates from the bottom up, then the shared outline.</summary>
    private void SortOutlineTargets()
    {
        var sorted = OutlineTargets.OrderBy(t => t.Plate is null ? int.MaxValue : (int)t.Plate).ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            var index = OutlineTargets.IndexOf(sorted[i]);
            if (index != i)
                OutlineTargets.Move(index, i);
        }
    }

    /// <summary>The labels of the outline choice in the new language.</summary>
    private void RefreshOutlineTargets()
    {
        var selected = SelectedOutlineTarget?.Plate;
        _isApplyingOutline = true;
        try
        {
            var targets = OutlineTargets.Select(t => t.Plate).ToList();
            OutlineTargets.Clear();
            foreach (var plate in targets)
                OutlineTargets.Add(new OutlineTarget(plate));
            SelectedOutlineTarget = OutlineTargets.FirstOrDefault(t => t.Plate == selected);
        }
        finally
        {
            _isApplyingOutline = false;
        }

        // The names of the plates in the legend of the board view
        UpdateOutlineState();
    }

    partial void OnOutlineModeChanged(PlateOutlineMode value)
    {
        // An outline of the other file type does not fit the new choice
        if (!_isApplyingOutline && value is PlateOutlineMode.Dxf or PlateOutlineMode.Svg && _importedMode != value)
        {
            _importedOutline = null;
            OutlineFile = null;
        }

        if (value == PlateOutlineMode.BoardLayer && OutlineLayer is null)
            OutlineLayer = OutlineLayerNames.FirstOrDefault();

        OnOutlineChanged();
    }

    partial void OnPlateMarginChanged(double value) => OnOutlineChanged();
    partial void OnOutlineWidthChanged(double value) => OnOutlineChanged();
    partial void OnOutlineHeightChanged(double value) => OnOutlineChanged();
    partial void OnOutlineOffsetXChanged(double value) => OnOutlineChanged();
    partial void OnOutlineOffsetYChanged(double value) => OnOutlineChanged();
    partial void OnOutlineLayerChanged(string? value) => OnOutlineChanged();

    private void OnOutlineChanged()
    {
        if (_isApplyingOutline)
            return;

        MarkDirty();
        RunChecks();
    }

    /// <summary>
    /// The outline a plate gets, in build sheet coordinates. Without a usable outline of the chosen kind, the board
    /// outline plus the margin, and the status says so.
    /// </summary>
    private OutlineShape CurrentOutline(FixturePlate plate)
    {
        var board = Board!;
        var state = OutlineOf(plate);
        return SelectedOutline(board, state) ?? PlateOutlines.Automatic(board, state.Margin);
    }

    private OutlineShape? SelectedOutline(Board board, OutlineState s) => s.Mode switch
    {
        PlateOutlineMode.FixedSize when s.Width > 0 && s.Height > 0 =>
            PlateOutlines.Centered(board, OutlineShape.Rectangle(s.Width, s.Height), s.OffsetX, s.OffsetY),
        PlateOutlineMode.Dxf or PlateOutlineMode.Svg when s.Imported is not null =>
            PlateOutlines.Centered(board, s.Imported, s.OffsetX, s.OffsetY),
        PlateOutlineMode.BoardLayer when s.Layer is not null =>
            PlateOutlines.FromBoardLayer(board, s.Layer, Localizer.Get),
        PlateOutlineMode.Automatic => PlateOutlines.Automatic(board, s.Margin),
        _ => null
    };

    /// <summary>Status text and the outline over the board, after every change of the outline or the board.</summary>
    private void UpdateOutlineState()
    {
        if (Board is not { } board)
        {
            OutlineStatus = string.Empty;
            OutlineOverlay = null;
            OutlineHighlight = null;
            return;
        }

        SyncShapeRows();
        var outline = SelectedOutline(board, CaptureOutline());
        IsOutlineMissing = outline is null;
        var shown = outline ?? PlateOutlines.Automatic(board, PlateMargin);

        var lines = new List<string>();
        if (outline is null)
        {
            lines.Add(OutlineMode == PlateOutlineMode.BoardLayer && OutlineLayer is not null
                ? Localizer.Format("Outline_NoLayerOutline", OutlineLayer)
                : Localizer.Get("Outline_Missing"));
        }
        else
        {
            var culture = CultureInfo.CurrentCulture;
            lines.Add(Localizer.Format("Outline_Status", shown.Width.ToString("0.0", culture), shown.Height.ToString("0.0", culture),
                shown.Circles.Count, shown.Cutouts.Count));
            lines.AddRange(outline.Warnings);
        }

        OutlineStatus = string.Join("\n", lines);

        // The board view shows the outlines of all plates with their pockets shaded and their loose lines
        OutlineOverlay = PlateOverlays(board, shown);
        OutlineHighlight = SelectedOutlineShape is { } selected && ReferenceEquals(outline, shown) && IsOutlineImported
            ? selected.IsCircle
                ? selected.Index < shown.Circles.Count ? CirclePoints(board, shown.Circles[selected.Index]) : null
                : selected.Index < shown.Cutouts.Count ? LoopPoints(board, shown.Cutouts[selected.Index].Vertices) : null
            : null;
    }

    /// <summary>
    /// The outlines of the plates of the fixture for the board view, drawn as in the build sheet: each plate in its color,
    /// an outline that several plates share with its dashes in their colors by turns. A pocket or loose line is drawn
    /// once, in the color of the first plate that has it, as the shading would otherwise add up. An outline being edited
    /// that no plate of the fixture has is shown as well.
    /// </summary>
    private List<BoardOverlayOutline> PlateOverlays(Board board, OutlineShape edited)
    {
        var plates = Plate.PlatesOf(Probes, IsBoardFlipped, IsTopPlateCombined);
        var shapes = plates.ToDictionary(p => p, p => CurrentOutline(p));
        var drawings = BoardSvg.GroupByOutline(plates.Select(p => new PlateOutlineDrawing(p, Overlay(board, shapes[p]))).ToList())
            .SelectMany(group => group.Select((drawing, index) => (drawing, index, group.Count)))
            .OrderBy(o => o.drawing.Plate);

        var drawn = new List<IReadOnlyList<Point2>>();
        List<IReadOnlyList<Point2>> NotDrawnYet(List<IReadOnlyList<Point2>> lines)
        {
            var fresh = lines.Where(l => !drawn.Any(d => d.SequenceEqual(l))).ToList();
            drawn.AddRange(fresh);
            return fresh;
        }

        var overlays = new List<BoardOverlayOutline>();
        foreach (var (drawing, index, count) in drawings)
        {
            var shape = shapes[drawing.Plate];
            overlays.Add(new BoardOverlayOutline(PlateRow.NameOf(drawing.Plate), Color.Parse(BoardSvg.PlateColor(drawing.Plate)),
                drawing.Outline, index, count, NotDrawnYet(PocketsOf(board, shape)), NotDrawnYet(LooseOf(board, shape))));
        }

        var isUsed = _editedOutline is { } plate ? plates.Contains(plate) : plates.Any(p => !_ownOutlines.ContainsKey(p));
        if (!isUsed)
            overlays.Add(new BoardOverlayOutline(null, Color.Parse(FixtureColors.Get(_editedOutline is { } own ? FixtureColors.Of(own) : ColorItem.ProbePlate)),
                Overlay(board, edited), Fills: PocketsOf(board, edited), Guides: LooseOf(board, edited)));

        return overlays;
    }

    /// <summary>The lines of an outline that belong to no shape, in board coordinates.</summary>
    private static List<IReadOnlyList<Point2>> LooseOf(Board board, OutlineShape shape) =>
        shape.Loose.Select(piece => PiecePoints(board, piece)).ToList();

    /// <summary>The pockets of an outline in board coordinates.</summary>
    private static List<IReadOnlyList<Point2>> PocketsOf(Board board, OutlineShape shape) =>
        shape.Circles.Where(c => c.Pocket is not null).Select(c => CirclePoints(board, c))
            .Concat(shape.Cutouts.Where(c => c.Pocket is not null).Select(c => LoopPoints(board, c.Vertices)))
            .ToList();

    /// <summary>Build sheet coordinates back to board coordinates.</summary>
    private static Point2 ToBoard(Board board, (double X, double Y) p) => new(p.X + board.Origin.X, board.Origin.Y - p.Y);

    private static IReadOnlyList<Point2> CirclePoints(Board board, OutlineCircle c) =>
        OutlineBuilder.Arc(c.X, c.Y, c.Diameter / 2, 0, 2 * Math.PI).Select(p => ToBoard(board, p)).ToList();

    private static IReadOnlyList<Point2> LoopPoints(Board board, IReadOnlyList<OutlineVertex> loop)
    {
        var points = OutlineShape.Flatten(loop).Select(p => ToBoard(board, p)).ToList();
        points.Add(points[0]);
        return points;
    }

    private static IReadOnlyList<Point2> PiecePoints(Board board, IReadOnlyList<OutlineVertex> piece)
    {
        var points = new List<Point2>();
        for (var i = 0; i < piece.Count; i++)
        {
            if (i + 1 < piece.Count && Math.Abs(piece[i].Bulge) > 1e-12)
                points.AddRange(OutlineShape.ArcBetween(piece[i], piece[i + 1]).SkipLast(1).Select(p => ToBoard(board, p)));
            else
                points.Add(ToBoard(board, (piece[i].X, piece[i].Y)));
        }

        return points;
    }

    /// <summary>An outline with its frame holes and cut-outs in board coordinates, as the board view and the build sheet draw it.</summary>
    private static IReadOnlyList<IReadOnlyList<Point2>> Overlay(Board board, OutlineShape shape)
    {
        // Build sheet coordinates back to board coordinates
        Point2 ToBoard((double X, double Y) p) => new(p.X + board.Origin.X, board.Origin.Y - p.Y);
        var overlay = new List<IReadOnlyList<Point2>>();
        var loop = shape.Points.Select(ToBoard).ToList();
        loop.Add(loop[0]);
        overlay.Add(loop);
        foreach (var circle in shape.Circles)
            overlay.Add(OutlineBuilder.Arc(circle.X, circle.Y, circle.Diameter / 2, 0, 2 * Math.PI).Select(ToBoard).ToList());
        foreach (var cutout in shape.Cutouts)
        {
            var points = OutlineShape.Flatten(cutout.Vertices).Select(ToBoard).ToList();
            points.Add(points[0]);
            overlay.Add(points);
        }

        return overlay;
    }

    /// <summary>The outline of a plate over the board, for its drawings in the build sheet.</summary>
    private IReadOnlyList<IReadOnlyList<Point2>>? PlateOverlay(FixturePlate plate) => Board is { } board ? Overlay(board, CurrentOutline(plate)) : null;

    // ----- Import -----

    [RelayCommand]
    private async Task ChooseOutlineFile()
    {
        if (Dialogs is null || !IsOutlineImported)
            return;

        if (await Dialogs.OpenOutlineAsync(OutlineMode == PlateOutlineMode.Svg) is { } path)
            await ImportOutlineAsync(path);
    }

    [RelayCommand]
    private async Task ReloadOutlineFile()
    {
        if (OutlineFile is not null)
            await ImportOutlineAsync(OutlineFile);
    }

    private async Task ImportOutlineAsync(string path)
    {
        var warnings = new List<string>();
        OutlineShape? shape;
        try
        {
            shape = OutlineMode == PlateOutlineMode.Svg
                ? SvgOutlineReader.Read(path, Localizer.Get, warnings)
                : DxfOutlineReader.Read(path, Localizer.Get, warnings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await ShowErrorAsync(Localizer.Get("Error_Outline"), ex.Message);
            return;
        }

        if (shape is null)
        {
            await ShowErrorAsync(Localizer.Get("Error_Outline"), Localizer.Format("Outline_None", Path.GetFileName(path)));
            return;
        }

        // Shapes that were pockets before stay pockets when the file is read again
        var before = _importedMode == OutlineMode ? _importedOutline : null;
        _importedOutline = new OutlineShape
        {
            Vertices = shape.Vertices, Circles = shape.Circles, Cutouts = shape.Cutouts, Loose = shape.Loose,
            Warnings = [.. warnings, .. shape.Warnings]
        };
        if (before is not null)
            _importedOutline = KeepPockets(_importedOutline, before);
        _importedMode = OutlineMode;
        OutlineFile = path;
        OnOutlineChanged();
    }

    /// <summary>The pockets of the shapes of an earlier import given to the same shapes (within 0.01 mm) of a new one.</summary>
    private static OutlineShape KeepPockets(OutlineShape shape, OutlineShape before)
    {
        static bool Near(double a, double b) => Math.Abs(a - b) <= 0.01;
        Pocket? CirclePocket(int i)
        {
            var c = shape.Circles[i];
            return before.Circles.FirstOrDefault(o => o.Pocket is not null && Near(o.X, c.X) && Near(o.Y, c.Y) && Near(o.Diameter, c.Diameter)).Pocket;
        }

        Pocket? CutoutPocket(int i)
        {
            var c = shape.Cutouts[i].Vertices;
            return before.Cutouts.FirstOrDefault(o => o.Pocket is not null && o.Vertices.Count == c.Count &&
                                                      o.Vertices.Zip(c).All(p => Near(p.First.X, p.Second.X) && Near(p.First.Y, p.Second.Y) &&
                                                                                 Math.Abs(p.First.Bulge - p.Second.Bulge) < 1e-6))?.Pocket;
        }

        return shape.WithPockets(CirclePocket, CutoutPocket);
    }

    // ----- File -----

    private void ResetOutline() => LoadOutline(null, null, string.Empty);

    /// <summary>
    /// The outlines of a file: the plates' own, the lowest of which is edited first, and the shared one, which may be used
    /// by no plate and is therefore only edited first when no plate has its own.
    /// </summary>
    private void LoadOutline(PlateOutlineEntry? shared, IReadOnlyDictionary<FixturePlate, PlateOutlineEntry>? own, string fixturePath)
    {
        var sharedState = StateOf(shared, fixturePath);
        _isApplyingOutline = true;
        try
        {
            _ownOutlines.Clear();
            foreach (var (plate, entry) in own ?? new Dictionary<FixturePlate, PlateOutlineEntry>())
                _ownOutlines[plate] = StateOf(entry, fixturePath);

            OutlineTargets.Clear();
            foreach (var plate in _ownOutlines.Keys.Order())
                OutlineTargets.Add(new OutlineTarget(plate));
            OutlineTargets.Add(new OutlineTarget(null));
            foreach (var row in PlateRows)
                row.HasOwnOutline = _ownOutlines.ContainsKey(row.Plate);

            _editedOutline = OutlineTargets[0].Plate;
            _sharedOutline = sharedState;
            SelectedOutlineTarget = OutlineTargets[0];
        }
        finally
        {
            _isApplyingOutline = false;
        }

        OnPropertyChanged(nameof(HasOwnOutlines));
        ApplyOutline(_editedOutline is { } edited ? _ownOutlines[edited] : sharedState);
    }

    private static OutlineState StateOf(PlateOutlineEntry? entry, string fixturePath)
    {
        if (entry is null)
            return new OutlineState();

        // The imported outline as it was saved; the file itself is only needed to read it again
        OutlineShape? imported = null;
        string? file = null;
        if (entry.Mode is PlateOutlineMode.Dxf or PlateOutlineMode.Svg && entry.Points is { Count: >= 3 } points)
        {
            var pockets = entry.Pockets ?? [];
            Pocket? PocketOf(bool circle, int index) =>
                pockets.FirstOrDefault(p => p.IsCircle == circle && p.Index == index && p.Depth > 0) is { } p ? new Pocket(p.Depth, p.Side) : null;

            imported = new OutlineShape
            {
                Vertices = points.Where(p => p.Length >= 2).Select(Vertex).ToList(),
                Circles = (entry.Circles ?? []).Where(c => c.Length >= 3).Select((c, i) => new OutlineCircle(c[0], c[1], c[2], PocketOf(true, i))).ToList(),
                Cutouts = (entry.Cutouts ?? []).Where(c => c.Count >= 2)
                    .Select((c, i) => new OutlineCutout(c.Where(p => p.Length >= 2).Select(Vertex).ToList(), PocketOf(false, i))).ToList(),
                Loose = (entry.Loose ?? []).Where(l => l.Count >= 2)
                    .Select(l => (IReadOnlyList<OutlineVertex>)l.Where(p => p.Length >= 2).Select(Vertex).ToList()).ToList()
            };
            file = entry.SourceFile is { Length: > 0 } source ? FixtureFile.ResolveStoredPath(source, fixturePath) : null;
        }

        return new OutlineState
        {
            Mode = entry.Mode,
            Margin = entry.Margin,
            Width = entry.Width > 0 ? entry.Width : 200,
            Height = entry.Height > 0 ? entry.Height : 150,
            OffsetX = entry.OffsetX,
            OffsetY = entry.OffsetY,
            Layer = entry.Layer,
            Imported = imported,
            ImportedMode = imported is null ? PlateOutlineMode.Automatic : entry.Mode,
            File = file
        };
    }

    /// <summary>A point of a saved outline: [x, y] or [x, y, bulge], the bulge of the edge to the next point.</summary>
    private static OutlineVertex Vertex(double[] p) => new(p[0], p[1], p.Length >= 3 ? p[2] : 0);

    private static double[] Stored(OutlineVertex v) => v.Bulge == 0
        ? [Math.Round(v.X, 4), Math.Round(v.Y, 4)]
        : [Math.Round(v.X, 4), Math.Round(v.Y, 4), Math.Round(v.Bulge, 8)];

    /// <summary>The shared outline (null for the automatic one) and the plates' own.</summary>
    private (PlateOutlineEntry? Shared, Dictionary<FixturePlate, PlateOutlineEntry>? Own) SaveOutline(string fixturePath) =>
        (EntryOf(StateOf(null), fixturePath, always: false),
            _ownOutlines.Count == 0 ? null : _ownOutlines.Keys.ToDictionary(p => p, p => EntryOf(StateOf(p), fixturePath, always: true)!));

    private static PlateOutlineEntry? EntryOf(OutlineState s, string fixturePath, bool always)
    {
        if (s.Mode == PlateOutlineMode.Automatic && s.Margin == FixtureFile.DefaultPlateMargin && !always)
            return null;

        var entry = new PlateOutlineEntry
        {
            Mode = s.Mode,
            Margin = s.Margin,
            Width = s.Width,
            Height = s.Height,
            OffsetX = s.OffsetX,
            OffsetY = s.OffsetY,
            Layer = s.Layer
        };

        if (s.Mode is PlateOutlineMode.Dxf or PlateOutlineMode.Svg && s.Imported is { } imported)
        {
            entry.SourceFile = s.File is null ? null : FixtureFile.ToStoredPath(s.File, fixturePath);
            entry.Points = imported.Vertices.Select(Stored).ToList();
            entry.Cutouts = imported.Cutouts.Count > 0 ? imported.Cutouts.Select(c => c.Vertices.Select(Stored).ToList()).ToList() : null;
            entry.Circles = imported.Circles.Select(c => new[] { Math.Round(c.X, 4), Math.Round(c.Y, 4), Math.Round(c.Diameter, 4) }).ToList();
            entry.Loose = imported.Loose.Count > 0 ? imported.Loose.Select(l => l.Select(Stored).ToList()).ToList() : null;
            var pockets = imported.Circles.Select((c, i) => (c.Pocket, IsCircle: true, Index: i))
                .Concat(imported.Cutouts.Select((c, i) => (c.Pocket, IsCircle: false, Index: i)))
                .Where(p => p.Pocket is not null)
                .Select(p => new PlatePocketEntry { IsCircle = p.IsCircle, Index = p.Index, Depth = Math.Round(p.Pocket!.Value.Depth, 4), Side = p.Pocket.Value.Side })
                .ToList();
            entry.Pockets = pockets.Count > 0 ? pockets : null;
        }

        return entry;
    }
}