using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATLab.FixtureDesigner.Localization;
using ATLab.FixtureDesigner.Plates;
using ATLab.Pcb.Fixture;
using ATLab.Pcb.Kicad;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.FixtureDesigner.ViewModels;

/// <summary>A way to define the plate outline, as offered in the Manufacturing tab.</summary>
public sealed record OutlineModeOption(PlateOutlineMode Mode, string LabelKey)
{
    public string Label => Localizer.Get(LabelKey);
}

/// <summary>
/// The outline of the probe plates: the board outline plus the margin, a fixed size, an outline imported from a DXF or
/// SVG file, or one drawn on a layer of the board. A fixed size or an imported outline is centered on the board and
/// moved by the offset. Both plates share one outline, or each has its own: then the properties below are those of the
/// plate being edited, and the other plate's are kept aside until the user switches to it.
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
        nameof(IsOutlineImported), nameof(IsOutlineBoardLayer), nameof(HasOutlineOffset))]
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

    /// <summary>The outline in board coordinates, drawn over the board so its position can be checked.</summary>
    [ObservableProperty] private IReadOnlyList<IReadOnlyList<Point2>>? _outlineOverlay;

    // ----- One outline for both plates, or one each -----

    /// <summary>The top plate has an outline of its own; otherwise both plates share one.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditingTopOutline), nameof(IsEditingBottomOutline))]
    private bool _isSeparateOutlines;

    /// <summary>The plate whose outline the properties above show, while each plate has its own.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditingTopOutline), nameof(IsEditingBottomOutline))]
    private BoardSide _editedOutlineSide = BoardSide.Bottom;

    public bool IsEditingTopOutline
    {
        get => IsSeparateOutlines && EditedOutlineSide == BoardSide.Top;
        set
        {
            if (value)
                EditedOutlineSide = BoardSide.Top;
        }
    }

    public bool IsEditingBottomOutline
    {
        get => IsSeparateOutlines && EditedOutlineSide == BoardSide.Bottom;
        set
        {
            if (value)
                EditedOutlineSide = BoardSide.Bottom;
        }
    }

    /// <summary>The settings of an outline, as kept for the plate that is not being edited.</summary>
    private sealed class OutlineState
    {
        public PlateOutlineMode Mode { get; init; }
        public double Width { get; init; } = 200;
        public double Height { get; init; } = 150;
        public double OffsetX { get; init; }
        public double OffsetY { get; init; }
        public string? File { get; init; }
        public string? Layer { get; init; }
        public OutlineShape? Imported { get; init; }
        public PlateOutlineMode ImportedMode { get; init; }
    }

    /// <summary>The outline of the plate that is not being edited, while each plate has its own.</summary>
    private OutlineState? _otherOutline;

    /// <summary>True while the properties are filled from a kept outline: no change of the user.</summary>
    private bool _isApplyingOutline;

    private OutlineState CaptureOutline() => new()
    {
        Mode = OutlineMode,
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

    /// <summary>
    /// Switched on, the top plate starts with the outline both plates had; switched off, both get the outline being
    /// edited.
    /// </summary>
    partial void OnIsSeparateOutlinesChanged(bool value)
    {
        _otherOutline = value ? CaptureOutline() : null;
        if (!value)
            EditedOutlineSide = BoardSide.Bottom;
        if (!_isApplyingOutline)
            OnOutlineChanged();
    }

    /// <summary>The outline of the other plate comes into the properties, the edited one is kept aside.</summary>
    partial void OnEditedOutlineSideChanged(BoardSide oldValue, BoardSide newValue)
    {
        if (!IsSeparateOutlines || _otherOutline is null || _isApplyingOutline)
            return;

        var edited = CaptureOutline();
        ApplyOutline(_otherOutline);
        _otherOutline = edited;
        UpdatePlateSummary();
    }

    /// <summary>The outline settings of a plate: the edited ones, or for the other plate those kept aside.</summary>
    private OutlineState OutlineOf(BoardSide side) =>
        IsSeparateOutlines && side != EditedOutlineSide && _otherOutline is not null ? _otherOutline : CaptureOutline();

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
        UpdatePlateSummary();
    }

    /// <summary>
    /// The outline a plate gets, in build sheet coordinates. Without a usable outline of the chosen kind, the board
    /// outline plus the margin, and the status says so.
    /// </summary>
    private OutlineShape CurrentOutline(BoardSide side)
    {
        var board = Board!;
        return SelectedOutline(board, OutlineOf(side)) ?? PlateOutlines.Automatic(board, PlateMargin);
    }

    private OutlineShape? SelectedOutline(Board board, OutlineState s) => s.Mode switch
    {
        PlateOutlineMode.FixedSize when s.Width > 0 && s.Height > 0 =>
            PlateOutlines.Centered(board, OutlineShape.Rectangle(s.Width, s.Height), s.OffsetX, s.OffsetY),
        PlateOutlineMode.Dxf or PlateOutlineMode.Svg when s.Imported is not null =>
            PlateOutlines.Centered(board, s.Imported, s.OffsetX, s.OffsetY),
        PlateOutlineMode.BoardLayer when s.Layer is not null =>
            PlateOutlines.FromBoardLayer(board, s.Layer, Localizer.Get),
        PlateOutlineMode.Automatic => PlateOutlines.Automatic(board, PlateMargin),
        _ => null
    };

    /// <summary>Status text and the outline over the board, after every change of the outline or the board.</summary>
    private void UpdateOutlineState()
    {
        if (Board is not { } board)
        {
            OutlineStatus = string.Empty;
            OutlineOverlay = null;
            return;
        }

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

        // The board view shows the outline being edited
        OutlineOverlay = Overlay(board, shown);
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
            var points = OutlineShape.Flatten(cutout).Select(ToBoard).ToList();
            points.Add(points[0]);
            overlay.Add(points);
        }

        return overlay;
    }

    /// <summary>The outline of a plate over the board, for its drawings in the build sheet.</summary>
    private IReadOnlyList<IReadOnlyList<Point2>>? PlateOverlay(BoardSide side) => Board is { } board ? Overlay(board, CurrentOutline(side)) : null;

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

        _importedOutline = new OutlineShape
        {
            Vertices = shape.Vertices, Circles = shape.Circles, Cutouts = shape.Cutouts, Warnings = [.. warnings, .. shape.Warnings]
        };
        _importedMode = OutlineMode;
        OutlineFile = path;
        OnOutlineChanged();
    }

    // ----- File -----

    private void ResetOutline()
    {
        _isApplyingOutline = true;
        try
        {
            IsSeparateOutlines = false;
            EditedOutlineSide = BoardSide.Bottom;
        }
        finally
        {
            _isApplyingOutline = false;
        }

        _otherOutline = null;
        ApplyOutline(new OutlineState());
    }

    /// <summary>
    /// The outlines of a file: the bottom plate's (and the top plate's while it has none of its own), and the top
    /// plate's if it has one. The bottom plate's is the one edited first.
    /// </summary>
    private void LoadOutline(PlateOutlineEntry? entry, PlateOutlineEntry? topEntry, string fixturePath)
    {
        ResetOutline();
        var bottom = StateOf(entry, fixturePath);
        if (topEntry is not null)
        {
            _isApplyingOutline = true;
            try
            {
                IsSeparateOutlines = true;
            }
            finally
            {
                _isApplyingOutline = false;
            }

            _otherOutline = StateOf(topEntry, fixturePath);
        }

        ApplyOutline(bottom);
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
            imported = new OutlineShape
            {
                Vertices = points.Where(p => p.Length >= 2).Select(Vertex).ToList(),
                Circles = (entry.Circles ?? []).Where(c => c.Length >= 3).Select(c => new OutlineCircle(c[0], c[1], c[2])).ToList(),
                Cutouts = (entry.Cutouts ?? []).Where(c => c.Count >= 2)
                    .Select(c => (IReadOnlyList<OutlineVertex>)c.Where(p => p.Length >= 2).Select(Vertex).ToList()).ToList()
            };
            file = entry.SourceFile is { Length: > 0 } source ? FixtureFile.ResolveStoredPath(source, fixturePath) : null;
        }

        return new OutlineState
        {
            Mode = entry.Mode,
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

    /// <summary>The outline of the bottom plate (and of the top plate while it shares it), and the top plate's own.</summary>
    private (PlateOutlineEntry? Bottom, PlateOutlineEntry? Top) SaveOutline(string fixturePath) =>
        IsSeparateOutlines
            ? (EntryOf(OutlineOf(BoardSide.Bottom), fixturePath, always: true), EntryOf(OutlineOf(BoardSide.Top), fixturePath, always: true))
            : (EntryOf(CaptureOutline(), fixturePath, always: false), null);

    private static PlateOutlineEntry? EntryOf(OutlineState s, string fixturePath, bool always)
    {
        if (s.Mode == PlateOutlineMode.Automatic && !always)
            return null;

        var entry = new PlateOutlineEntry
        {
            Mode = s.Mode,
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
            entry.Cutouts = imported.Cutouts.Count > 0 ? imported.Cutouts.Select(c => c.Select(Stored).ToList()).ToList() : null;
            entry.Circles = imported.Circles.Select(c => new[] { Math.Round(c.X, 4), Math.Round(c.Y, 4), Math.Round(c.Diameter, 4) }).ToList();
        }

        return entry;
    }
}