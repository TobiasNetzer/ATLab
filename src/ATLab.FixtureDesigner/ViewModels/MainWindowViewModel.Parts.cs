using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATLab.FixtureDesigner.Fixture;
using ATLab.FixtureDesigner.Localization;
using ATLab.Pcb.Controls;
using ATLab.Pcb.Fixture;
using ATLab.Pcb.Kicad;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.FixtureDesigner.ViewModels;

/// <summary>A kind of part for the type list's selection.</summary>
public sealed record PartKindOption(FixturePartKind Kind)
{
    public string Label => Localizer.Get(FixturePart.KindKey(Kind));
}

/// <summary>A tip of a pin or rod for the type's selection.</summary>
public sealed record PartTipOption(PartTipStyle Style)
{
    public string Label => Localizer.Get("Tip_" + Style);
}

/// <summary>
/// Tooling, pre-centering and support pins and push rods: placed with a click on the board, moved by dragging or by
/// typing X and Y, with types like the probe types. Tooling pins snap into a hole of the board, pre-centering pins to the
/// board edge.
/// </summary>
public partial class MainWindowViewModel
{
    private readonly HashSet<FixturePart> _observedParts = [];
    private readonly HashSet<PartType> _observedPartTypes = [];

    /// <summary>True while a part's board and build sheet positions are being matched, so they do not chase each other.</summary>
    private bool _isSyncingPart;

    public ObservableCollection<FixturePart> Parts { get; } = [];
    public ObservableCollection<PartType> PartTypes { get; } = [];

    public IReadOnlyList<PartKindOption> PartKinds { get; } =
        Enum.GetValues<FixturePartKind>().Select(k => new PartKindOption(k)).ToList();

    public IReadOnlyList<PartTipOption> TipOptions { get; } =
        Enum.GetValues<PartTipStyle>().Select(s => new PartTipOption(s)).ToList();

    /// <summary>Types of the user's library, offered by "Add from library".</summary>
    [ObservableProperty] private IReadOnlyList<PartType> _libraryPartTypes = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemovePartCommand), nameof(DeleteSelectedCommand))]
    [NotifyPropertyChangedFor(nameof(SelectedPartMarker))]
    private FixturePart? _selectedPart;

    partial void OnSelectedPartChanged(FixturePart? value)
    {
        if (value is not null)
            _isPartSelectedLast = true;
    }

    /// <summary>The selected part as the board view sees it.</summary>
    public IBoardMarker? SelectedPartMarker
    {
        get => SelectedPart;
        set => SelectedPart = value as FixturePart;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemovePartTypeCommand), nameof(SavePartTypeToLibraryCommand))]
    private PartType? _selectedPartType;

    /// <summary>The kind of part a click on the board places; null when not placing.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPlacing), nameof(IsPlacingToolingPin), nameof(IsPlacingPreCenteringPin), nameof(IsPlacingSupportPin),
        nameof(IsPlacingPushRod))]
    private FixturePartKind? _placingKind;

    public bool IsPlacing => PlacingKind is not null;
    public bool IsPlacingToolingPin => PlacingKind == FixturePartKind.ToolingPin;
    public bool IsPlacingPreCenteringPin => PlacingKind == FixturePartKind.PreCenteringPin;
    public bool IsPlacingSupportPin => PlacingKind == FixturePartKind.SupportPin;
    public bool IsPlacingPushRod => PlacingKind == FixturePartKind.PushRod;

    public bool HasParts => Parts.Count > 0;

    private void InitializeParts()
    {
        // Like the probes: Clear() reports no old items, so compare after every change
        Parts.CollectionChanged += (_, _) =>
        {
            Sync(Parts, _observedParts, OnPartChanged);
            OnPropertyChanged(nameof(HasParts));
        };
        PartTypes.CollectionChanged += (_, _) =>
        {
            Sync(PartTypes, _observedPartTypes, OnPartTypeChanged);
            UpdateAvailableTypes();
        };

        LibraryPartTypes = PartLibrary.Load();
        foreach (var type in LibraryPartTypes)
            PartTypes.Add(type.Copy());
    }

    // ----- Placing -----

    [RelayCommand]
    private void PlaceToolingPin() => TogglePlacing(FixturePartKind.ToolingPin);

    [RelayCommand]
    private void PlacePreCenteringPin() => TogglePlacing(FixturePartKind.PreCenteringPin);

    [RelayCommand]
    private void PlaceSupportPin() => TogglePlacing(FixturePartKind.SupportPin);

    [RelayCommand]
    private void PlacePushRod() => TogglePlacing(FixturePartKind.PushRod);

    private void TogglePlacing(FixturePartKind kind)
    {
        PlacingKind = PlacingKind == kind || Board is null ? null : kind;
        if (PlacingKind is { } placing)
            SetStatus(() => Localizer.Format("Status_Placing", Localizer.Get(FixturePart.KindKey(placing))));
    }

    /// <summary>
    /// A click on the board in place mode: a new part of the chosen kind, with the first type of that kind. A tooling pin
    /// needs a hole near the click.
    /// </summary>
    [RelayCommand]
    private void PlaceAt(Point2 boardPoint)
    {
        if (PlacingKind is not { } kind || Board is null)
            return;

        if (kind == FixturePartKind.ToolingPin && BoardHoles.Near(Board, boardPoint) is null)
        {
            SetStatus(() => Localizer.Get("Status_ToolingNeedsHole"));
            return;
        }

        // A fixture without a type of this kind gets the one of the library
        if (PartTypes.All(t => t.Kind != kind) && LibraryPartTypes.FirstOrDefault(t => t.Kind == kind) is { } libraryType)
            PartTypes.Add(libraryType.Copy());

        var part = new FixturePart
        {
            Kind = kind,
            Number = Parts.Where(p => p.Kind == kind).Select(p => p.Number).DefaultIfEmpty(0).Max() + 1,
            Type = PartTypes.FirstOrDefault(t => t.Kind == kind)
        };
        part.AvailableTypes = TypesOf(kind);
        part.MoveTo(boardPoint);
        Snap(part);
        SyncFromBoard(part);
        Parts.Add(part);
        SelectedPart = part;
        AfterPartsChanged();
        SetStatus(() => Localizer.Format("Status_PartPlaced", part.Name, Localizer.Get(FixturePart.KindKey(kind))));
    }

    /// <summary>A part was dragged: a tooling pin snaps into a hole, a pre-centering pin to the board edge again.</summary>
    [RelayCommand]
    private void PartMoved(IBoardMarker? marker)
    {
        if (marker is not FixturePart part)
            return;

        Snap(part);
        SyncFromBoard(part);
        AfterPartsChanged();
    }

    private bool HasSelectedPart() => SelectedPart is not null;

    [RelayCommand(CanExecute = nameof(HasSelectedPart))]
    private void RemovePart()
    {
        if (SelectedPart is not { } part)
            return;

        var index = Parts.IndexOf(part);
        Parts.Remove(part);

        // Each kind stays numbered without gaps
        var number = 1;
        foreach (var other in Parts.Where(p => p.Kind == part.Kind).OrderBy(p => p.Number))
            other.Number = number++;

        // The next row moves up into the selection, or the one before when the last row went
        SelectedPart = Parts.Count == 0 ? null : Parts[Math.Min(index, Parts.Count - 1)];
        AfterPartsChanged();
        SetStatus(() => Localizer.Format("Status_PartRemoved", part.Name));
    }

    /// <summary>Ends place mode (Escape).</summary>
    private bool CancelPlacing()
    {
        if (PlacingKind is null)
            return false;

        PlacingKind = null;
        return true;
    }

    // ----- Position -----

    /// <summary>
    /// A tooling pin moves into the nearest hole, or back into its last one if there is none nearby; a pre-centering pin
    /// moves beside the board edge, its type's gap away from it; other parts stay where they are.
    /// </summary>
    private void Snap(FixturePart part)
    {
        if (Board is null)
            return;

        var position = new Point2(part.BoardX, part.BoardY);
        switch (part.Kind)
        {
            case FixturePartKind.ToolingPin:
                if (BoardHoles.Near(Board, position) is { } hole)
                {
                    part.MoveTo(hole.Position);
                    part.Anchor = hole.Position;
                }
                else if (part.Anchor is { } anchor)
                {
                    part.MoveTo(anchor);
                    SetStatus(() => Localizer.Get("Status_ToolingNeedsHole"));
                }

                break;

            case FixturePartKind.PreCenteringPin:
                part.MoveTo(BoardEdge.Snap(Board, position, (part.Type?.Diameter ?? 0) / 2 + Math.Max(part.Type?.EdgeGap ?? 0, 0)));
                break;
        }
    }

    /// <summary>Build sheet coordinates from the board position.</summary>
    private void SyncFromBoard(FixturePart part)
    {
        if (Board is null)
            return;

        _isSyncingPart = true;
        try
        {
            var p = Board.ToFixture(new Point2(part.BoardX, part.BoardY));
            part.X = Math.Round(p.X, 3);
            part.Y = Math.Round(p.Y, 3);
        }
        finally
        {
            _isSyncingPart = false;
        }
    }

    private void OnPartChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not FixturePart part || _isSyncingPart)
            return;

        switch (e.PropertyName)
        {
            // Typed in the list: the board position follows (no snapping, the user means this point)
            case nameof(FixturePart.X) or nameof(FixturePart.Y) when Board is not null:
                _isSyncingPart = true;
                try
                {
                    part.MoveTo(new Point2(part.X + Board.Origin.X, Board.Origin.Y - part.Y));
                }
                finally
                {
                    _isSyncingPart = false;
                }

                AfterPartsChanged();
                break;

            // While dragging only the board position changes; the list follows
            case nameof(FixturePart.BoardX) or nameof(FixturePart.BoardY):
                SyncFromBoard(part);
                break;

            case nameof(FixturePart.OpeningOverride):
                AfterPartsChanged();
                break;

            // The hole of a tooling pin does not depend on its type
            case nameof(FixturePart.Type) when part.Kind == FixturePartKind.ToolingPin:
                AfterPartsChanged();
                break;

            case nameof(FixturePart.Type):
                Snap(part);
                SyncFromBoard(part);
                AfterPartsChanged();
                break;
        }
    }

    private void AfterPartsChanged()
    {
        MarkDirty();
        RunChecks();
    }

    /// <summary>
    /// After a new board: the build sheet positions of the parts from their board positions; a tooling pin keeps its
    /// hole as the place to go back to, if the board still has it.
    /// </summary>
    private void ResolveParts()
    {
        foreach (var part in Parts)
        {
            if (part.Kind == FixturePartKind.ToolingPin && Board is not null)
                part.Anchor = BoardHoles.At(Board, new Point2(part.BoardX, part.BoardY))?.Position;
            SyncFromBoard(part);
        }
    }

    // ----- Types -----

    private IReadOnlyList<PartType> TypesOf(FixturePartKind kind) => PartTypes.Where(t => t.Kind == kind).ToList();

    private void UpdateAvailableTypes()
    {
        foreach (var part in Parts)
            part.AvailableTypes = TypesOf(part.Kind);
    }

    private void OnPartTypeChanged(object? sender, PropertyChangedEventArgs e)
    {
        MarkDirty();
        if (e.PropertyName == nameof(PartType.Kind))
            UpdateAvailableTypes();
        if (e.PropertyName is nameof(PartType.OpeningDiameter) or nameof(PartType.DrillDiameter))
        {
            foreach (var part in Parts.Where(p => p.Type == sender))
                part.RefreshOpening();
        }
        if (e.PropertyName is nameof(PartType.Diameter) or nameof(PartType.EdgeGap) && sender is PartType type)
        {
            // A pre-centering pin of another size or gap stands at another place
            foreach (var part in Parts.Where(p => p.Type == type && p.Kind == FixturePartKind.PreCenteringPin))
            {
                Snap(part);
                SyncFromBoard(part);
            }
        }

        if (e.PropertyName != nameof(PartType.Note))
            RunChecks();
    }

    [RelayCommand]
    private void AddPartType()
    {
        var template = SelectedPartType;
        var type = template?.Copy(newId: true) ?? new PartType { Kind = FixturePartKind.SupportPin, Diameter = 4, DrillDiameter = 4 };
        type.Name = Localizer.Get("Text_NewPartType");
        type.Note = string.Empty;
        PartTypes.Add(type);
        SelectedPartType = type;
        MarkDirty();
    }

    private bool HasSelectedPartType() => SelectedPartType is not null;

    /// <summary>Removes the type; its parts get another type of their kind, or none.</summary>
    [RelayCommand(CanExecute = nameof(HasSelectedPartType))]
    private void RemovePartType()
    {
        if (SelectedPartType is not { } type)
            return;

        var index = PartTypes.IndexOf(type);
        PartTypes.Remove(type);
        foreach (var part in Parts.Where(p => p.Type == type))
            part.Type = PartTypes.FirstOrDefault(t => t.Kind == part.Kind);

        SelectedPartType = PartTypes.Count == 0 ? null : PartTypes[Math.Min(index, PartTypes.Count - 1)];
        MarkDirty();
        RunChecks();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedPartType))]
    private async Task SavePartTypeToLibrary()
    {
        if (SelectedPartType is not { } type)
            return;

        try
        {
            PartLibrary.Store(type);
            LibraryPartTypes = PartLibrary.Load();
            SetStatus(() => Localizer.Format("Status_TypeStored", type.Name));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await ShowErrorAsync(Localizer.Get("Error_Library"), ex.Message);
        }
    }

    /// <summary>Adds a type of the library to the fixture, or updates the fixture's copy of it.</summary>
    [RelayCommand]
    private void AddPartTypeFromLibrary(PartType? libraryType)
    {
        if (libraryType is null)
            return;

        if (PartTypes.FirstOrDefault(t => t.Id == libraryType.Id) is { } existing)
        {
            existing.Kind = libraryType.Kind;
            existing.Name = libraryType.Name;
            existing.Diameter = libraryType.Diameter;
            existing.DrillDiameter = libraryType.DrillDiameter;
            existing.OpeningDiameter = libraryType.OpeningDiameter;
            existing.EdgeGap = libraryType.EdgeGap;
            existing.Height = libraryType.Height;
            existing.BodyDiameter = libraryType.BodyDiameter;
            existing.ThinLength = libraryType.ThinLength;
            existing.StepChamfer = libraryType.StepChamfer;
            existing.TipStyle = libraryType.TipStyle;
            existing.TipSize = libraryType.TipSize;
            existing.IsPrinted = libraryType.IsPrinted;
            existing.ComponentClearance = libraryType.ComponentClearance;
            existing.Note = libraryType.Note;
            SelectedPartType = existing;
        }
        else
        {
            var copy = libraryType.Copy();
            PartTypes.Add(copy);
            SelectedPartType = copy;
        }

        MarkDirty();
        RunChecks();
    }

    // ----- File -----

    private void ResetParts()
    {
        PlacingKind = null;
        SelectedPart = null;
        Parts.Clear();
        PartTypes.Clear();
        LibraryPartTypes = PartLibrary.Load();
        foreach (var type in LibraryPartTypes)
            PartTypes.Add(type.Copy());
        SelectedPartType = null;
    }

    /// <summary>Types and parts of a file; a file without types gets the types of the library.</summary>
    private void LoadParts(FixtureFile file)
    {
        PlacingKind = null;
        SelectedPart = null;
        Parts.Clear();
        PartTypes.Clear();
        LibraryPartTypes = PartLibrary.Load();
        foreach (var type in file.PartTypes.Count > 0 ? file.PartTypes : LibraryPartTypes.Select(t => t.Copy()).ToList())
            PartTypes.Add(type);
        SelectedPartType = null;

        foreach (var entry in file.Parts.OrderBy(p => p.Kind).ThenBy(p => p.Number))
        {
            var part = new FixturePart
            {
                Kind = entry.Kind,
                Number = entry.Number,
                Type = PartTypes.FirstOrDefault(t => t.Id == entry.Type && t.Kind == entry.Kind)
                       ?? PartTypes.FirstOrDefault(t => t.Kind == entry.Kind),
                BoardX = entry.X,
                BoardY = entry.Y,
                OpeningOverride = entry.Kind == FixturePartKind.PushRod && entry.Opening is > 0 ? entry.Opening : null
            };
            part.AvailableTypes = TypesOf(entry.Kind);
            if (entry.Kind == FixturePartKind.ToolingPin)
                part.Anchor = new Point2(entry.X, entry.Y);
            Parts.Add(part);
        }
    }

    private void SaveParts(FixtureFile file)
    {
        file.PartTypes = PartTypes.ToList();
        file.Parts = Parts.OrderBy(p => p.Kind).ThenBy(p => p.Number).Select(p => new FixturePartEntry
        {
            Number = p.Number,
            Kind = p.Kind,
            Type = p.Type?.Id,
            X = Math.Round(p.BoardX, 4),
            Y = Math.Round(p.BoardY, 4),
            Opening = p.HasOpening ? p.OpeningOverride : null
        }).ToList();
    }
}