using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Pcb.Controls;
using ATLab.FixtureDesigner.Fixture;
using ATLab.Pcb.Fixture;
using ATLab.Pcb.Kicad;
using ATLab.Pcb.Rendering;
using ATLab.FixtureDesigner.Localization;
using ATLab.FixtureDesigner.Plates;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.FixtureDesigner.ViewModels;

/// <summary>Probe types, plate files and the layers of the board view.</summary>
public partial class MainWindowViewModel
{
    private readonly HashSet<ProbeType> _observedTypes = [];

    private void InitializePlates()
    {
        // Like the probes: Clear() reports no old items, so compare after every change
        ProbeTypes.CollectionChanged += (_, _) => Sync(ProbeTypes, _observedTypes, OnProbeTypeChanged);

        LibraryTypes = ProbeLibrary.Load();
        foreach (var type in LibraryTypes)
            ProbeTypes.Add(type.Copy());
        DefaultProbeType = ProbeTypes.FirstOrDefault();

        BuildLayerOptions();
        BuildSheetLayers.Changed += MarkDirty;
        BuildSheetLayers.DefaultSaved += OnBuildSheetLayersDefaultSaved;
    }

    private static void Sync<T>(IEnumerable<T> items, HashSet<T> observed, PropertyChangedEventHandler handler)
        where T : INotifyPropertyChanged
    {
        var current = items.ToHashSet();
        foreach (var item in observed.Where(i => !current.Contains(i)).ToList())
        {
            item.PropertyChanged -= handler;
            observed.Remove(item);
        }

        foreach (var item in current.Where(observed.Add))
            item.PropertyChanged += handler;
    }

    // ----- Probe types -----

    public ObservableCollection<ProbeType> ProbeTypes { get; } = [];

    /// <summary>Type new probes get.</summary>
    [ObservableProperty] private ProbeType? _defaultProbeType;

    partial void OnDefaultProbeTypeChanged(ProbeType? value) => MarkDirty();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveProbeTypeCommand), nameof(SaveTypeToLibraryCommand), nameof(ApplyTypeToAllCommand))]
    private ProbeType? _selectedProbeType;

    /// <summary>Types of the user's library, offered by "Add from library".</summary>
    [ObservableProperty] private IReadOnlyList<ProbeType> _libraryTypes = [];

    private void OnProbeTypeChanged(object? sender, PropertyChangedEventArgs e)
    {
        MarkDirty();
        if (e.PropertyName != nameof(ProbeType.Note))
            RunChecks();
    }

    /// <summary>Type of a probe in a file; the default type if the ID is unknown (or the file has no types).</summary>
    private ProbeType? TypeOf(string? id) => ProbeTypes.FirstOrDefault(t => t.Id == id) ?? DefaultProbeType;

    [RelayCommand]
    private void AddProbeType()
    {
        var template = SelectedProbeType ?? DefaultProbeType;
        var type = template?.Copy(newId: true) ?? new ProbeType();
        type.Name = Localizer.Get("Text_NewProbeType");
        type.Note = string.Empty;
        ProbeTypes.Add(type);
        DefaultProbeType ??= type;
        SelectedProbeType = type;
        MarkDirty();
    }

    private bool CanRemoveProbeType() => SelectedProbeType is not null && ProbeTypes.Count > 1;

    /// <summary>Removes the type; its probes get the default type (another one if the default is removed).</summary>
    [RelayCommand(CanExecute = nameof(CanRemoveProbeType))]
    private void RemoveProbeType()
    {
        if (SelectedProbeType is not { } type)
            return;

        var index = ProbeTypes.IndexOf(type);
        ProbeTypes.Remove(type);
        if (DefaultProbeType == type)
            DefaultProbeType = ProbeTypes.FirstOrDefault();

        _isBatchChange = true;
        try
        {
            foreach (var probe in Probes.Where(p => p.Type == type))
                probe.Type = DefaultProbeType;
        }
        finally
        {
            _isBatchChange = false;
        }

        SelectedProbeType = ProbeTypes.Count == 0 ? null : ProbeTypes[Math.Clamp(index, 0, ProbeTypes.Count - 1)];
        MarkDirty();
        RunChecks();
    }

    private bool HasSelectedProbeType() => SelectedProbeType is not null;

    /// <summary>Gives every probe the selected type, e.g. after choosing the probes for the whole fixture.</summary>
    [RelayCommand(CanExecute = nameof(HasSelectedProbeType))]
    private void ApplyTypeToAll()
    {
        if (SelectedProbeType is not { } type)
            return;

        _isBatchChange = true;
        try
        {
            foreach (var probe in Probes)
                probe.Type = type;
        }
        finally
        {
            _isBatchChange = false;
        }

        RunChecks();
        SetStatus(() => Localizer.Format("Status_TypeApplied", type.Name, Probes.Count));
    }

    /// <summary>Stores the selected type in the user's library, for other fixtures.</summary>
    [RelayCommand(CanExecute = nameof(HasSelectedProbeType))]
    private async Task SaveTypeToLibrary()
    {
        if (SelectedProbeType is not { } type)
            return;

        try
        {
            ProbeLibrary.Store(type);
            LibraryTypes = ProbeLibrary.Load();
            SetStatus(() => Localizer.Format("Status_TypeStored", type.Name));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await ShowErrorAsync(Localizer.Get("Error_Library"), ex.Message);
        }
    }

    /// <summary>Adds a type of the library to the fixture, or updates the fixture's copy of it.</summary>
    [RelayCommand]
    private void AddFromLibrary(ProbeType? libraryType)
    {
        if (libraryType is null)
            return;

        if (ProbeTypes.FirstOrDefault(t => t.Id == libraryType.Id) is { } existing)
        {
            existing.Name = libraryType.Name;
            existing.MinPadSize = libraryType.MinPadSize;
            existing.Diameter = libraryType.Diameter;
            existing.DrillDiameter = libraryType.DrillDiameter;
            existing.Height = libraryType.Height;
            existing.MinSpacing = libraryType.MinSpacing;
            existing.ComponentClearance = libraryType.ComponentClearance;
            existing.Note = libraryType.Note;
            SelectedProbeType = existing;
        }
        else
        {
            var copy = libraryType.Copy();
            ProbeTypes.Add(copy);
            SelectedProbeType = copy;
        }

        MarkDirty();
        RunChecks();
    }

    // ----- Plates -----

    /// <summary>Distance of the plate outline from the board outline, mm.</summary>
    [ObservableProperty] private double _plateMargin = FixtureFile.DefaultPlateMargin;

    /// <summary>Thickness of the plates in the STEP files, mm.</summary>
    [ObservableProperty] private double _plateThickness = FixtureFile.DefaultPlateThickness;

    partial void OnPlateMarginChanged(double value)
    {
        MarkDirty();
        UpdatePlateSummary();
    }

    partial void OnPlateThicknessChanged(double value) => MarkDirty();

    /// <summary>Which plates the plate files contain, with their holes and problems.</summary>
    [ObservableProperty] private string _plateSummary = string.Empty;

    [ObservableProperty] private IReadOnlyList<string> _plateProblems = [];

    public bool HasPlateProblems => PlateProblems.Count > 0;

    partial void OnPlateProblemsChanged(IReadOnlyList<string> value) => OnPropertyChanged(nameof(HasPlateProblems));

    // ----- What the export writes -----

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportPlatesCommand))]
    private bool _isExportDxf = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportPlatesCommand))]
    private bool _isExportStep = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportPlatesCommand))]
    private bool _isExportSvg;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportPlatesCommand))]
    private bool _isExportTopPlate = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportPlatesCommand))]
    private bool _isExportBottomPlate = true;

    /// <summary>Which plates the fixture has; only those can be chosen for the export.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportPlatesCommand))]
    private bool _hasTopPlate;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportPlatesCommand))]
    private bool _hasBottomPlate;

    partial void OnIsExportDxfChanged(bool value) => SaveExportChoice();
    partial void OnIsExportStepChanged(bool value) => SaveExportChoice();
    partial void OnIsExportSvgChanged(bool value) => SaveExportChoice();

    /// <summary>The file types are kept for the next fixture; the plates depend on the fixture.</summary>
    private void SaveExportChoice()
    {
        if (_settings is null)
            return;

        _settings.IsExportDxf = IsExportDxf;
        _settings.IsExportStep = IsExportStep;
        _settings.IsExportSvg = IsExportSvg;
        _settings.Save();
    }

    private bool IsSideExported(BoardSide side) => side == BoardSide.Top ? IsExportTopPlate : IsExportBottomPlate;

    private bool CanExportPlates() =>
        Board is not null &&
        (IsExportDxf || IsExportStep || IsExportSvg) &&
        ((HasTopPlate && IsExportTopPlate) || (HasBottomPlate && IsExportBottomPlate));

    private void UpdatePlateSummary()
    {
        if (Board is null)
        {
            PlateSummary = string.Empty;
            PlateProblems = [];
            HasTopPlate = false;
            HasBottomPlate = false;
            return;
        }

        var plates = Plate.Build(Board, Probes, CurrentOutline, Parts);
        UpdateOutlineState();
        HasTopPlate = plates.Any(p => p.Side == BoardSide.Top);
        HasBottomPlate = plates.Any(p => p.Side == BoardSide.Bottom);
        PlateSummary = plates.Count == 0
            ? Localizer.Get("Text_NoPlates")
            : string.Join("\n", plates.Select(p => Localizer.Format("Text_PlateSummary", PlateName(p.Side),
                p.Width.ToString("0.0", System.Globalization.CultureInfo.CurrentCulture),
                p.Height.ToString("0.0", System.Globalization.CultureInfo.CurrentCulture),
                p.Holes.Count(h => h.Kind == PlateHoleKind.Probe), p.Holes.Count(h => h.Kind == PlateHoleKind.LocatingPin),
                p.Holes.Count(h => h.Kind == PlateHoleKind.Frame),
                p.Holes.Count(h => h.Kind is PlateHoleKind.PreCenteringPin or PlateHoleKind.SupportPin or PlateHoleKind.PushRod),
                p.Cutouts.Count)));
        PlateProblems = plates.SelectMany(p => p.Problems.Select(problem => $"{PlateName(p.Side)}: {problem}")).ToList();
    }

    private static string PlateName(BoardSide side) => Localizer.Get(side == BoardSide.Top ? "Plate_Top" : "Plate_Bottom");

    /// <summary>
    /// Writes the chosen manufacturing files of the chosen plates into a folder: drawing (DXF and SVG)
    /// and 3D model (STEP). Plates with overlapping holes or holes at the edge are not written, since they cannot be
    /// made as drawn.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExportPlates))]
    private async Task ExportPlates()
    {
        if (Dialogs is null || Board is null)
            return;

        var plates = Plate.Build(Board, Probes, CurrentOutline, Parts).Where(p => IsSideExported(p.Side)).ToList();
        if (plates.Count == 0)
        {
            await ShowErrorAsync(Localizer.Get("Error_Plates"), Localizer.Get("Text_NoPlates"));
            return;
        }

        var problems = plates.SelectMany(p => p.Problems.Select(problem => $"{PlateName(p.Side)}: {problem}")).ToList();
        if (problems.Count > 0)
        {
            await ShowErrorAsync(Localizer.Get("Error_Plates"),
                Localizer.Format("Error_PlateProblems", string.Join("\n", problems.Take(12)) + (problems.Count > 12 ? "\n…" : "")));
            return;
        }

        if (await Dialogs.OpenFolderAsync(Localizer.Get("Dialog_PlateFolder")) is not { } folder)
            return;

        var baseName = SafeFileName(FixturePath is not null ? Path.GetFileNameWithoutExtension(FixturePath) : Name.Length > 0 ? Name : "Fixture");
        var files = plates.SelectMany(p =>
        {
            var name = $"{baseName} - {Localizer.Get(p.Side == BoardSide.Top ? "File_TopPlate" : "File_BottomPlate")}";
            var extensions = new List<string>();
            if (IsExportDxf) extensions.Add(".dxf");
            if (IsExportStep) extensions.Add(".step");
            if (IsExportSvg) extensions.Add(".svg");
            return extensions.Select(extension => (Plate: p, Name: name, Path: Path.Combine(folder, name + extension)));
        }).ToList();

        var existing = files.Where(f => File.Exists(f.Path)).ToList();
        if (existing.Count > 0)
        {
            var choice = await AskAsync(Localizer.Get("Dialog_OverwriteTitle"),
                Localizer.Format("Dialog_Overwrite", string.Join("\n", existing.Select(f => Path.GetFileName(f.Path)))),
                [new(Localizer.Get("Button_Overwrite"), DialogResult.Yes, true), new(Localizer.Get("Button_Cancel"), DialogResult.Cancel)]);
            if (choice != DialogResult.Yes)
                return;
        }

        try
        {
            var title = Name.Length > 0 ? Name : baseName;
            var thickness = PlateThickness > 0 ? PlateThickness : FixtureFile.DefaultPlateThickness;
            await Task.Run(() =>
            {
                foreach (var (plate, name, path) in files)
                {
                    if (path.EndsWith(".dxf", StringComparison.Ordinal))
                        DxfWriter.Save(plate, path);
                    else if (path.EndsWith(".svg", StringComparison.Ordinal))
                        SvgWriter.Save(plate, path);
                    else
                        StepWriter.Save(plate, thickness, name, path);
                }
            });
            SetStatus(() => Localizer.Format("Status_PlatesSaved", files.Count, folder));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await ShowErrorAsync(Localizer.Get("Error_Plates"), ex.Message);
        }
    }

    // ----- File -----

    /// <summary>A new fixture: the types of the library, the default plate settings.</summary>
    private void ResetPlates()
    {
        ProbeTypes.Clear();
        LibraryTypes = ProbeLibrary.Load();
        foreach (var type in LibraryTypes)
            ProbeTypes.Add(type.Copy());
        DefaultProbeType = ProbeTypes.FirstOrDefault();
        SelectedProbeType = null;
        PlateMargin = FixtureFile.DefaultPlateMargin;
        PlateThickness = FixtureFile.DefaultPlateThickness;
        ResetOutline();
        ResetParts();
        _buildSheetLayerSettings = new();
        LoadBuildSheetLayers();
    }

    /// <summary>Types, parts and plate settings of a file; a file of version 1 gets the types of the library.</summary>
    private void LoadPlates(FixtureFile file, string fixturePath)
    {
        ProbeTypes.Clear();
        LibraryTypes = ProbeLibrary.Load();

        var types = file.ProbeTypes.Count > 0 ? file.ProbeTypes : LibraryTypes.Select(t => t.Copy()).ToList();
        foreach (var type in types)
            ProbeTypes.Add(type);
        DefaultProbeType = ProbeTypes.FirstOrDefault(t => t.Id == file.DefaultProbeType) ?? ProbeTypes.FirstOrDefault();
        SelectedProbeType = null;

        PlateMargin = file.PlateMargin;
        PlateThickness = file.PlateThickness > 0 ? file.PlateThickness : FixtureFile.DefaultPlateThickness;
        LoadOutline(file.BottomPlateOutline, file.TopPlateOutline, fixturePath);
        LoadParts(file);
        _buildSheetLayerSettings = file.BuildSheetLayers?.Copy() ?? new();
        LoadBuildSheetLayers();
    }

    private void SavePlates(FixtureFile file, string fixturePath)
    {
        file.ProbeTypes = ProbeTypes.ToList();
        file.DefaultProbeType = DefaultProbeType?.Id;
        file.PlateMargin = PlateMargin;
        file.PlateThickness = PlateThickness;
        (file.BottomPlateOutline, file.TopPlateOutline) = SaveOutline(fixturePath);
        file.BuildSheetLayers = BuildSheetLayers.Settings.Copy();
        SaveParts(file);
    }

    // ----- Layers of the build sheet -----

    /// <summary>
    /// Layers of the build sheet drawings: drawn when the plate faces the top or the bottom side, with their order,
    /// color and opacity, like the board pictures of ATLab's test report. Kept in the fixture.
    /// </summary>
    public ReportLayerList BuildSheetLayers { get; } = new(name => name switch
        {
            ATLab.Pcb.Controls.LayerColors.TopPads => Localizer.Get("Layer_TopPads"),
            ATLab.Pcb.Controls.LayerColors.BottomPads => Localizer.Get("Layer_BottomPads"),
            ATLab.Pcb.Controls.LayerColors.Holes => Localizer.Get("Layer_Holes"),
            _ => name
        },
        [
            SheetView(Reporting.BuildSheet.AssemblyTop, "LayerView_AssemblyTop", "Report_AssemblyTop"),
            SheetView(Reporting.BuildSheet.AssemblyBottom, "LayerView_AssemblyBottom", "Report_AssemblyBottom"),
            SheetView(Reporting.BuildSheet.WiringTop, "LayerView_WiringTop", "Report_WiringTop"),
            SheetView(Reporting.BuildSheet.WiringBottom, "LayerView_WiringBottom", "Report_WiringBottom")
        ]);

    private static ReportLayerView SheetView(string key, string label, string toolTip) =>
        new(key, () => Localizer.Get(label), Reporting.BuildSheet.DefaultLayers(key), () => Localizer.Get(toolTip));

    private ReportLayerSettings _buildSheetLayerSettings = new();

    private void LoadBuildSheetLayers()
    {
        BuildSheetLayers.UserDefault = _settings?.DefaultBuildSheetLayers;
        BuildSheetLayers.Load(Board, _buildSheetLayerSettings);
    }

    /// <summary>"Save as default": kept in the Designer's settings for new fixtures.</summary>
    private void OnBuildSheetLayersDefaultSaved(ReportLayerSettings layers)
    {
        if (_settings is null)
            return;

        _settings.DefaultBuildSheetLayers = layers;
        _settings.Save();
        SetStatus(() => Localizer.Get("Status_LayersDefaultSaved"));
    }

    // ----- Layers of the board view -----

    /// <summary>Layers a new installation shows: pads, holes and the silkscreen.</summary>
    private static readonly string[] DefaultLayers = [ATLab.Pcb.Controls.LayerColors.TopPads, ATLab.Pcb.Controls.LayerColors.BottomPads, ATLab.Pcb.Controls.LayerColors.Holes, "F.SilkS", "B.SilkS"];

    public ObservableCollection<LayerOption> LayerOptions { get; } = [];

    [ObservableProperty] private ISet<string>? _visibleLayers;

    /// <summary>Colors the user chose for KiCad layers, for the board view.</summary>
    [ObservableProperty] private IReadOnlyDictionary<string, Avalonia.Media.Color>? _layerColors;

    /// <summary>The layers of the board, plus pads and holes; visibility and colors as last chosen by the user.</summary>
    private void BuildLayerOptions()
    {
        var visible = (_settings?.VisibleLayers ?? [.. DefaultLayers]).ToHashSet();
        _layerColorHex = _settings?.LayerColors is { } stored ? new(stored) : [];
        LayerColors = ATLab.Pcb.Controls.LayerColors.Parse(_layerColorHex);
        LayerOptions.Clear();

        var names = new List<string> { ATLab.Pcb.Controls.LayerColors.TopPads, ATLab.Pcb.Controls.LayerColors.BottomPads, ATLab.Pcb.Controls.LayerColors.Holes };
        names.AddRange(Board?.Layers ?? []);
        foreach (var name in names)
        {
            var option = new LayerOption(name, IsDarkMode) { IsVisible = visible.Contains(name) };
            ApplyColors(option, IsDarkMode, LayerColors);
            option.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(LayerOption.IsVisible))
                    OnLayerVisibilityChanged();
                else if (e.PropertyName == nameof(LayerOption.Color) && !option.IsSettingColor)
                    OnLayerColorChanged(option.Name, option.Color);
            };
            LayerOptions.Add(option);
        }

        VisibleLayers = visible;
    }

    partial void OnBoardChanged(Board? value)
    {
        BuildLayerOptions();
        LoadBuildSheetLayers();
        OnPropertyChanged(nameof(OutlineLayerNames));
    }

    private void OnLayerVisibilityChanged()
    {
        // Layers this board does not have keep their setting for other boards
        var visible = (_settings?.VisibleLayers ?? [.. DefaultLayers]).ToHashSet();
        foreach (var option in LayerOptions)
        {
            if (option.IsVisible)
                visible.Add(option.Name);
            else
                visible.Remove(option.Name);
        }

        VisibleLayers = visible;
        if (_settings is not null)
        {
            _settings.VisibleLayers = visible.Order().ToList();
            _settings.Save();
        }
    }

    [RelayCommand]
    private void ShowAllLayers(bool visible)
    {
        foreach (var option in LayerOptions)
            option.IsVisible = visible || option.Name is ATLab.Pcb.Controls.LayerColors.TopPads or ATLab.Pcb.Controls.LayerColors.BottomPads or ATLab.Pcb.Controls.LayerColors.Holes;
    }

    /// <summary>Colors of the layer list follow the theme; chosen colors stay.</summary>
    private void UpdateLayerColors(bool dark)
    {
        foreach (var option in LayerOptions)
            ApplyColors(option, dark, LayerColors);
    }

    /// <summary>A layer color chosen by the user, kept in the Designer's settings; the default color is not stored.</summary>
    private void OnLayerColorChanged(string key, Avalonia.Media.Color color)
    {
        if (color == ATLab.Pcb.Controls.LayerColors.Get(key, IsDarkMode))
            _layerColorHex.Remove(key);
        else
            _layerColorHex[key] = ATLab.Pcb.Controls.LayerColors.ToHex(color);
        LayerColors = ATLab.Pcb.Controls.LayerColors.Parse(_layerColorHex);
        if (_settings is null)
            return;

        _settings.LayerColors = _layerColorHex.Count > 0 ? new(_layerColorHex) : null;

        // The picker changes the color continuously while dragging; the file is written once it rests
        _saveLayerColorsTimer ??= new Avalonia.Threading.DispatcherTimer(TimeSpan.FromMilliseconds(600), Avalonia.Threading.DispatcherPriority.Background,
            (_, _) =>
            {
                _saveLayerColorsTimer!.Stop();
                _settings.Save();
            });
        _saveLayerColorsTimer.Stop();
        _saveLayerColorsTimer.Start();
    }

    private Avalonia.Threading.DispatcherTimer? _saveLayerColorsTimer;

    /// <summary>The chosen colors as stored ("#RRGGBB").</summary>
    private Dictionary<string, string> _layerColorHex = [];

    /// <summary>The color of a layer from the user's choice, else the theme's.</summary>
    private static void ApplyColors(LayerOption option, bool dark, IReadOnlyDictionary<string, Avalonia.Media.Color>? chosen) =>
        option.SetColor(ATLab.Pcb.Controls.LayerColors.Get(option.Name, dark, chosen));

    /// <summary>All layers back to their default colors.</summary>
    [RelayCommand]
    private void ResetLayerColors()
    {
        _layerColorHex.Clear();
        if (_settings is not null)
        {
            _settings.LayerColors = null;
            _settings.Save();
        }

        LayerColors = null;
        UpdateLayerColors(IsDarkMode);
    }
}

/// <summary>Layer in the layer list of the board view.</summary>
public partial class LayerOption : ObservableObject
{
    public LayerOption(string name, bool dark)
    {
        Name = name;
        _color = ATLab.Pcb.Controls.LayerColors.Get(name, dark);
    }

    public string Name { get; }

    /// <summary>KiCad name of the layer; the pseudo layers in the current language.</summary>
    public string Display => Name switch
    {
        ATLab.Pcb.Controls.LayerColors.TopPads => Localizer.Get("Layer_TopPads"),
        ATLab.Pcb.Controls.LayerColors.BottomPads => Localizer.Get("Layer_BottomPads"),
        ATLab.Pcb.Controls.LayerColors.Holes => Localizer.Get("Layer_Holes"),
        _ => Name
    };

    /// <summary>What the layer contains, e.g. "Top copper".</summary>
    public string Description => Name.StartsWith('@')
        ? string.Empty
        : Name.StartsWith("In", StringComparison.Ordinal) && Name.EndsWith(".Cu", StringComparison.Ordinal)
            ? Localizer.Format("Layer_Inner", Name[2..^3])
            : Localizer.Get("Layer_" + Name.Replace(".", "_")) is { } text && !text.StartsWith('[') ? text : string.Empty;

    [ObservableProperty] private bool _isVisible;

    /// <summary>Color of the layer in the board view; the color picker sets it, only for KiCad layers.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Brush))]
    private Avalonia.Media.Color _color;

    public Avalonia.Media.IBrush Brush => new Avalonia.Media.SolidColorBrush(Color);

    /// <summary>True while the color comes from the theme or the settings, not from the user.</summary>
    public bool IsSettingColor { get; private set; }

    /// <summary>A color from the theme or the settings, not a choice of the user.</summary>
    public void SetColor(Avalonia.Media.Color color)
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

    partial void OnColorChanged(Avalonia.Media.Color value)
    {
        if (value.A != 255)
            Color = Avalonia.Media.Color.FromRgb(value.R, value.G, value.B);
    }

    public void RefreshText()
    {
        OnPropertyChanged(nameof(Display));
        OnPropertyChanged(nameof(Description));
    }
}