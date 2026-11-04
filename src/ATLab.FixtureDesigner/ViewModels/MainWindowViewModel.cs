using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ATLab.FixtureDesigner.Fixture;
using ATLab.Pcb.Fixture;
using ATLab.Pcb.Kicad;
using ATLab.FixtureDesigner.Localization;
using ATLab.FixtureDesigner.Plates;
using ATLab.FixtureDesigner.Reporting;
using ATLab.FixtureDesigner.Services;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.FixtureDesigner.ViewModels;

/// <summary>
/// The fixture being edited: the KiCad board, the probes and their wiring to the adapter, the checks and the build
/// sheet.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly DesignerSettings? _settings;
    private readonly Dictionary<Pad, PadRow> _padRowLookup = [];
    private List<PadRow> _allPadRows = [];
    private bool _isLoading;
    private readonly HashSet<Probe> _observedProbes = [];

    /// <summary>Set while many probes change at once; the checks run once afterwards.</summary>
    private bool _isBatchChange;

    /// <summary>Builds the status text, so it can be rebuilt in another language.</summary>
    private Func<string> _status = () => Localizer.Get("Status_Start");

    /// <param name="settings">Language and theme to start with and to save changes to; null in tests.</param>
    public MainWindowViewModel(DesignerSettings? settings = null)
    {
        Languages = Localizer.Instance.SupportedCultures.Select(c => new LanguageOption(c)).ToList();
        if (settings?.Language is { } language)
            Localizer.Instance.ChangeCulture(new CultureInfo(language));
        _selectedLanguage = Languages.First(l => l.Culture.TwoLetterISOLanguageName == Localizer.Instance.Culture.TwoLetterISOLanguageName);

        _isDarkMode = settings?.IsDarkMode ?? true;
        _settings = settings;
        _isExportDxf = settings?.IsExportDxf ?? true;
        _isExportStep = settings?.IsExportStep ?? true;
        _isExportSvg = settings?.IsExportSvg ?? false;

        _pinout = AdapterPinout.Default;
        BuildPinLists();

        // Clear() reports no old items, so the subscriptions are compared with the collection after every change
        Probes.CollectionChanged += (_, _) =>
        {
            foreach (var probe in _observedProbes.Where(p => !Probes.Contains(p)).ToList())
            {
                probe.PropertyChanged -= OnProbeChanged;
                _observedProbes.Remove(probe);
            }

            foreach (var probe in Probes.Where(p => _observedProbes.Add(p)))
                probe.PropertyChanged += OnProbeChanged;
        };

        InitializePlates();
        InitializeParts();
        InitializeWiring();
        InitializeColors();
        Localizer.Instance.CultureChanged += (_, _) => OnCultureChanged();
    }

    public IDialogs? Dialogs { get; set; }

    // ----- Fixture -----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBoard), nameof(ShowWelcome), nameof(BoardInfo))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand), nameof(SaveAsCommand), nameof(ReloadBoardCommand),
        nameof(ExportBuildSheetCommand), nameof(AutoAssignCommand), nameof(RenumberCommand), nameof(ExportPlatesCommand),
        nameof(AddComponentCommand), nameof(AddWireCommand))]
    private Board? _board;

    public bool HasBoard => Board is not null;
    public bool ShowWelcome => Board is null;

    /// <summary>Path of the fixture file; null until it is saved.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle), nameof(FixtureFileName), nameof(FixturePathText))]
    private string? _fixturePath;

    public string FixturePathText => FixturePath ?? Localizer.Get("Text_Unsaved");

    public string FixtureFileName => FixturePath is null ? Localizer.Get("Text_Unsaved") : Path.GetFileName(FixturePath);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private bool _isDirty;

    public string WindowTitle
    {
        get
        {
            var name = FixturePath is not null ? Path.GetFileNameWithoutExtension(FixturePath)
                : Board is not null ? Localizer.Get("Text_NewFixture") : null;
            return name is null ? "ATLab Fixture Designer" : $"ATLab Fixture Designer - {name}{(IsDirty ? "*" : "")}";
        }
    }

    public string BoardInfo => Board is null
        ? string.Empty
        : Localizer.Format("Info_Board", Path.GetFileName(Board.FilePath), Board.Footprints.Count, Board.Pads.Count(),
            Board.GeneratorVersion.Length > 0 ? "KiCad " + Board.GeneratorVersion : Board.FormatVersion.ToString(CultureInfo.InvariantCulture));

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _deviceName = string.Empty;
    [ObservableProperty] private string _deviceRevision = string.Empty;
    [ObservableProperty] private string _author = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;

    partial void OnNameChanged(string value) => MarkDirty();
    partial void OnDeviceNameChanged(string value) => MarkDirty();
    partial void OnDeviceRevisionChanged(string value) => MarkDirty();
    partial void OnAuthorChanged(string value) => MarkDirty();
    partial void OnNotesChanged(string value) => MarkDirty();

    /// <summary>Hash of the board when the fixture was saved, to notice a changed board.</summary>
    private string _savedBoardHash = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdapterName))]
    private AdapterPinout _pinout;

    public string AdapterName => $"{Pinout.TestHardware.Name} ({Pinout.TestHardware.DeviceId})";

    public ObservableCollection<Probe> Probes { get; } = [];

    public bool HasProbes => Probes.Count > 0;

    private void MarkDirty()
    {
        if (!_isLoading && Board is not null)
            IsDirty = true;
    }

    // ----- Status -----

    public string StatusText => _status();

    private void SetStatus(Func<string> status)
    {
        _status = status;
        OnPropertyChanged(nameof(StatusText));
    }

    // ----- View -----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTopView), nameof(IsBottomView))]
    private BoardSide _viewSide = BoardSide.Bottom;

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

    [ObservableProperty] private int _selectedTabIndex;

    // ----- Pads -----

    [ObservableProperty] private IReadOnlyList<PadRow> _padRows = [];

    /// <summary>Pads of the filtered list, drawn in full color on the board.</summary>
    [ObservableProperty] private ISet<Pad>? _highlightedPads;

    [ObservableProperty] private string _padSearch = string.Empty;
    [ObservableProperty] private bool _testPointsOnly;
    [ObservableProperty] private bool _withNetOnly = true;

    partial void OnPadSearchChanged(string value) => ApplyPadFilter();
    partial void OnTestPointsOnlyChanged(bool value) => ApplyPadFilter();
    partial void OnWithNetOnlyChanged(bool value) => ApplyPadFilter();

    public string PadCountText => Localizer.Format("Text_PadCount", PadRows.Count, _allPadRows.Count);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddProbeTopCommand), nameof(AddProbeBottomCommand))]
    [NotifyPropertyChangedFor(nameof(SelectedPadRow))]
    private Pad? _selectedPad;

    /// <summary>Row of the selected pad in the pad list.</summary>
    public PadRow? SelectedPadRow
    {
        get => SelectedPad is { } pad && _padRowLookup.TryGetValue(pad, out var row) ? row : null;
        set
        {
            // A pad the filter hides cannot be selected in the list, which then clears its selection; the pad stays
            // selected on the board all the same
            if (value is null && SelectedPad is { } pad && !PadRows.Any(r => r.Pad == pad))
                return;

            SelectedPad = value?.Pad;
        }
    }

    partial void OnSelectedPadChanged(Pad? value)
    {
        // The probe selection follows the pad: a pad with a probe selects it, preferably the one of the viewed side;
        // a pad without a probe, or a click beside the pads, clears it, so the last probe does not stay selected
        var probes = value is null ? [] : Probes.Where(p => p.Pad == value).ToList();
        if (probes.Count == 0)
            SelectedProbe = null;
        else if (SelectedProbe?.Pad != value)
            SelectedProbe = probes.FirstOrDefault(p => p.Side == ViewSide) ?? probes[0];
    }

    private void BuildPadRows()
    {
        _padRowLookup.Clear();
        _allPadRows = Board?.Pads
            .OrderBy(p => p.Footprint.Reference, NaturalComparer.Instance)
            .ThenBy(p => p.Number, NaturalComparer.Instance)
            .Select(p => new PadRow(p))
            .ToList() ?? [];

        foreach (var row in _allPadRows)
            _padRowLookup[row.Pad] = row;

        // Boards with test points start with only them in the list
        _isLoading = true;
        TestPointsOnly = _allPadRows.Any(r => r.IsTestPoint);
        _isLoading = false;
        ApplyPadFilter();
    }

    private void ApplyPadFilter()
    {
        IEnumerable<PadRow> rows = _allPadRows;
        if (TestPointsOnly)
            rows = rows.Where(r => r.IsTestPoint);
        if (WithNetOnly)
            rows = rows.Where(r => r.Pad.HasNet);

        if (!string.IsNullOrWhiteSpace(PadSearch))
        {
            // Several terms must all match; * and ? are wildcards, e.g. "TP*" or "J1 GND"
            var matchers = PadSearch.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Matcher).ToList();
            rows = rows.Where(r => matchers.All(m =>
                m(r.Reference) || m($"{r.Reference}.{r.Number}") || m(r.Net) || m(r.FullNet) || m(r.Footprint)));
        }

        PadRows = rows.ToList();
        // Searched pads stand out on the board; the check boxes alone only shorten the list
        HighlightedPads = string.IsNullOrWhiteSpace(PadSearch) ? null : PadRows.Select(r => r.Pad).ToHashSet();
        OnPropertyChanged(nameof(PadCountText));
        OnPropertyChanged(nameof(SelectedPadRow));
    }

    private static Func<string, bool> Matcher(string term)
    {
        if (term.Contains('*') || term.Contains('?'))
        {
            var pattern = "^" + Regex.Escape(term).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return regex.IsMatch;
        }

        return text => text.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    // ----- Probes -----

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveProbeCommand), nameof(DisconnectProbeCommand), nameof(DeleteSelectedCommand))]
    private Probe? _selectedProbe;

    partial void OnSelectedProbeChanged(Probe? value)
    {
        if (value is not null)
            _isPartSelectedLast = false;
        if (value?.Pad is { } pad && SelectedPad != pad)
            SelectedPad = pad;
    }

    /// <summary>Pins for the selection boxes of the probe list.</summary>
    public IReadOnlyList<PinOption> PinOptions { get; private set; } = [];

    public IReadOnlyList<AdapterRow> AdapterRows { get; private set; } = [];

    private void BuildPinLists()
    {
        PinOptions = Pinout.FixturePins.Select(p => new PinOption(p)).ToList();
        AdapterRows = Pinout.FixturePins.Select(p => new AdapterRow(p)).ToList();
        OnPropertyChanged(nameof(PinOptions));
        OnPropertyChanged(nameof(AdapterRows));
    }

    private bool CanAddProbe() => SelectedPad is not null;

    [RelayCommand(CanExecute = nameof(CanAddProbe))]
    private void AddProbeTop() => AddProbe(SelectedPad!, BoardSide.Top);

    [RelayCommand(CanExecute = nameof(CanAddProbe))]
    private void AddProbeBottom() => AddProbe(SelectedPad!, BoardSide.Bottom);

    /// <summary>
    /// Double-click on a pad: removes the probe of the viewed side or adds one. A pad without copper on the viewed side
    /// gets its probe from the other side.
    /// </summary>
    [RelayCommand]
    private void TogglePad(Pad pad)
    {
        var side = pad.HasCopperOn(ViewSide) ? ViewSide : ViewSide == BoardSide.Top ? BoardSide.Bottom : BoardSide.Top;
        var existing = Probes.FirstOrDefault(p => p.Pad == pad && p.Side == side);
        if (existing is not null)
            RemoveProbeCore(existing, selectNext: false);
        else
            AddProbe(pad, side);
    }

    private void AddProbe(Pad pad, BoardSide side)
    {
        if (Probes.FirstOrDefault(p => p.Pad == pad && p.Side == side) is { } existing)
        {
            SelectedProbe = existing;
            return;
        }

        var probe = new Probe(pad.Footprint.Reference, pad.Number, side)
        {
            Number = Probes.Count == 0 ? 1 : Probes.Max(p => p.Number) + 1,
            Type = DefaultProbeType
        };
        Resolve(probe);
        Probes.Add(probe);
        SelectedProbe = probe;
        AfterProbesChanged();
        SetStatus(() => Localizer.Format("Status_ProbeAdded", probe.Name, probe.PadDisplay, Localizer.Get(side == BoardSide.Top ? "Side_Top" : "Side_Bottom")));
    }

    private bool HasSelectedProbe() => SelectedProbe is not null;

    [RelayCommand(CanExecute = nameof(HasSelectedProbe))]
    private void RemoveProbe(Probe? probe = null) => RemoveProbeCore(probe, selectNext: true);

    /// <summary>
    /// Removes the probe. From the list the next row is selected; on the board (selectNext false) the pad stays selected,
    /// the view does not jump to another probe.
    /// </summary>
    private void RemoveProbeCore(Probe? probe, bool selectNext)
    {
        probe ??= SelectedProbe;
        if (probe is null)
            return;

        var index = Probes.IndexOf(probe);
        RemoveWiresOf(probe);
        Probes.Remove(probe);
        if (selectNext)
            SelectedProbe = Probes.Count == 0 ? null : Probes[Math.Clamp(index, 0, Probes.Count - 1)];
        else if (SelectedProbe == probe)
            SelectedProbe = Probes.FirstOrDefault(p => p.Pad == probe.Pad && probe.Pad is not null);
        AfterProbesChanged();
        SetStatus(() => Localizer.Format("Status_ProbeRemoved", probe.Name));
    }

    [RelayCommand(CanExecute = nameof(HasSelectedProbe))]
    private void DisconnectProbe()
    {
        if (SelectedProbe is not { } probe)
            return;

        _isBatchChange = true;
        try
        {
            probe.Pin = null;
            RemoveWiresOf(probe);
        }
        finally
        {
            _isBatchChange = false;
        }

        AfterWiringChanged();
    }

    private bool CanEditProbes() => Board is not null;

    /// <summary>MEAS channel the automatic assignment leaves free; it is usually reserved.</summary>
    private const int ReservedMeasChannel = 1;

    /// <summary>
    /// Wires every probe without pin to a matrix channel: probes of a net that is already on a channel join it, the
    /// others get the next free channel from MEAS 2 on.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditProbes))]
    private void AutoAssign()
    {
        // MEAS 1 is usually reserved, so it is left for the user to assign by hand
        var meas = Pinout.FixturePins.Where(p => p.Kind == ResourceKind.Meas && p.Channel != ReservedMeasChannel).OrderBy(p => p.Channel).ToList();
        // Probes wired some other way, e.g. to a component, are left alone; pins on a wire are taken
        var wiring = FixtureWiringModel.Build(Probes, Wires);
        var used = Pinout.FixturePins.Where(wiring.IsWired).ToHashSet();
        var assigned = 0;
        var failed = 0;

        _isBatchChange = true;
        try
        {
            foreach (var probe in Probes.Where(p => p.Pin is null && !wiring.IsWired(p)).OrderBy(p => p.Number).ToList())
            {
                var sameNet = probe.Net.Length > 0
                    ? Probes.FirstOrDefault(p => p != probe && p.Net == probe.Net && p.Pin?.Kind == ResourceKind.Meas)?.Pin
                    : null;
                var pin = sameNet ?? meas.FirstOrDefault(p => !used.Contains(p));
                if (pin is null)
                {
                    failed++;
                    continue;
                }

                probe.Pin = pin;
                used.Add(pin);
                assigned++;
            }
        }
        finally
        {
            _isBatchChange = false;
        }

        RunChecks();
        SetStatus(() => failed == 0
            ? Localizer.Format("Status_AutoAssigned", assigned)
            : Localizer.Format("Status_AutoAssignedPartly", assigned, failed));
    }

    /// <summary>Numbers the probes by side, then row by row from the top left, as they are found on the drawing.</summary>
    [RelayCommand(CanExecute = nameof(CanEditProbes))]
    private void Renumber()
    {
        var ordered = Probes
            .OrderBy(p => p.Side)
            .ThenByDescending(p => Math.Round(p.Y, 1))
            .ThenBy(p => p.Side == BoardSide.Bottom ? -p.X : p.X)
            .ToList();

        // Sorted in place, so the list keeps its selection
        _isBatchChange = true;
        try
        {
            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i].Number = i + 1;
                var index = Probes.IndexOf(ordered[i]);
                if (index != i)
                    Probes.Move(index, i);
            }
        }
        finally
        {
            _isBatchChange = false;
        }

        BuildEndpointOptions();
        AfterProbesChanged();
        SetStatus(() => Localizer.Get("Status_Renumbered"));
    }

    private void OnProbeChanged(object? sender, PropertyChangedEventArgs e)
    {
        // The type sets the clearances and spacings the checks use
        if (e.PropertyName is nameof(Probe.Pin) or nameof(Probe.Type) or nameof(Probe.Note) or nameof(Probe.Number) or nameof(Probe.OpeningOverride))
        {
            MarkDirty();
            if (e.PropertyName != nameof(Probe.Note) && !_isBatchChange)
                RunChecks();
        }
    }

    private void AfterProbesChanged()
    {
        MarkDirty();
        OnPropertyChanged(nameof(HasProbes));
        OnPropertyChanged(nameof(ProbeCountText));
        BuildEndpointOptions();
        RunChecks();
    }

    public string ProbeCountText => Localizer.Format("Text_ProbeCount", Probes.Count,
        Probes.Count(p => p.Side == BoardSide.Top), Probes.Count(p => p.Side == BoardSide.Bottom));

    /// <summary>Connects the probe to the pad of the loaded board and computes its build sheet position.</summary>
    private void Resolve(Probe probe)
    {
        probe.Pad = Board?.FindPad(probe.Key);
        if (Board is not null && probe.Pad is { } pad)
        {
            var p = Board.ToFixture(pad.Position);
            probe.X = p.X;
            probe.Y = p.Y;
        }
    }

    // ----- Checks -----

    [ObservableProperty] private IReadOnlyList<CheckResult> _checks = [];

    [ObservableProperty] private CheckResult? _selectedCheck;

    partial void OnSelectedCheckChanged(CheckResult? value)
    {
        if (value?.Probes.Count > 0)
            SelectedProbe = value.Probes[0];
    }

    public int ErrorCount => Checks.Count(c => c.IsError);
    public int WarningCount => Checks.Count(c => c.IsWarning);
    public bool HasErrors => ErrorCount > 0;
    public bool HasWarnings => WarningCount > 0;
    public bool IsCheckClean => Board is not null && ErrorCount == 0 && WarningCount == 0;

    public string ChecksHeader => Checks.Count == 0
        ? Localizer.Get("Tab_Checks")
        : Localizer.Format("Tab_ChecksCount", Checks.Count);

    public string CheckSummary => Localizer.Format("Text_CheckSummary", ErrorCount, WarningCount);

    private void RunChecks()
    {
        var results = FixtureChecks.Run(Probes, Pinout, Board, Parts, Components, Wires);
        _wiring = FixtureWiringModel.Build(Probes, Wires);
        if (Board is not null && _savedBoardHash.Length > 0 && Board.Hash.Length > 0 && _savedBoardHash != Board.Hash)
            results.Add(new CheckResult(CheckSeverity.Info, Localizer.Get("Check_BoardChanged"), []));

        FixtureChecks.Mark(Probes, results);
        Checks = results;

        foreach (var name in new[] { nameof(ErrorCount), nameof(WarningCount), nameof(HasErrors), nameof(HasWarnings),
                     nameof(IsCheckClean), nameof(ChecksHeader), nameof(CheckSummary) })
            OnPropertyChanged(name);

        UpdateWiredTo();
        UpdateUsage();
        UpdatePlateSummary();
    }

    /// <summary>Which probes are on which pad and pin, for the pad list, the adapter list and the pin selection.</summary>
    private void UpdateUsage()
    {
        var byPad = Probes.Where(p => p.Pad is not null).GroupBy(p => p.Pad!).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var row in _allPadRows)
            row.Probes = byPad.TryGetValue(row.Pad, out var list) ? string.Join(", ", list.Select(p => p.Name)) : string.Empty;

        // A pin shows what is on its conductor: probes, component terminals, other pins; the net only of probes wired
        // to it directly, not of those behind a component
        foreach (var row in AdapterRows)
        {
            var net = _wiring.NetOf(row.Pin);
            row.Probes = net?.Describe(row.Pin) ?? string.Empty;
            var nets = net?.BoardNets.Select(Probe.ShortNetName).ToList() ?? [];
            row.Nets = string.Join(", ", nets);
            row.IsUsed = net is not null;
            row.IsShort = nets.Count > 1;
        }

        foreach (var option in PinOptions)
            option.UsedBy = _wiring.NetOf(option.Pin)?.Describe(option.Pin) ?? string.Empty;

        OnPropertyChanged(nameof(UsedPinCountText));
    }

    public string UsedPinCountText => Localizer.Format("Text_UsedPins", AdapterRows.Count(r => r.IsUsed), AdapterRows.Count,
        AdapterRows.Count(r => r.IsUsed && r.Pin.Kind == ResourceKind.Meas), Pinout.TestHardware.MeasChannels);

    // ----- Opening and saving -----

    /// <summary>A fixture file or a KiCad board, from the command line, drag &amp; drop or the open dialog.</summary>
    public async Task OpenPathAsync(string path)
    {
        if (path.EndsWith(".kicad_pcb", StringComparison.OrdinalIgnoreCase))
        {
            if (await ConfirmDiscardAsync())
                NewFromBoard(path);
        }
        else
        {
            if (await ConfirmDiscardAsync())
                await LoadFixtureAsync(path);
        }
    }

    [RelayCommand]
    private async Task Open()
    {
        if (Dialogs is null || await Dialogs.OpenFileAsync() is not { } path)
            return;
        await OpenPathAsync(path);
    }

    /// <summary>Starts a new fixture for a board.</summary>
    [RelayCommand]
    private async Task New()
    {
        if (!await ConfirmDiscardAsync() || Dialogs is null || await Dialogs.OpenBoardAsync() is not { } path)
            return;
        NewFromBoard(path);
    }

    private void NewFromBoard(string path)
    {
        if (ReadBoard(path) is not { } board)
            return;

        _isLoading = true;
        try
        {
            Probes.Clear();
            ResetPlates();
            Pinout = AdapterPinout.Default;
            BuildPinLists();
            ResetWiring();
            Board = board;
            FixturePath = null;
            _savedBoardHash = string.Empty;
            Name = board.Title;
            DeviceName = board.Title;
            DeviceRevision = board.Revision;
            Author = Environment.UserName;
            Notes = string.Empty;
            SelectedPad = null;
            SelectedProbe = null;
            BuildPadRows();
        }
        finally
        {
            _isLoading = false;
        }

        IsDirty = true;
        RunChecks();
        AfterFileChange();
        SetStatus(() => Localizer.Format("Status_BoardOpened", Path.GetFileName(path)));
    }

    private async Task LoadFixtureAsync(string path)
    {
        FixtureFile file;
        try
        {
            file = FixtureFile.Load(path);
        }
        catch (FixtureFileException ex)
        {
            await ShowErrorAsync(Localizer.Get("Error_OpenFixture"),
                Localizer.Format(ex.Error == FixtureFileError.Newer ? "Error_FixtureNewer" : "Error_FixtureInvalid", Path.GetFileName(path)));
            return;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await ShowErrorAsync(Localizer.Get("Error_OpenFixture"), ex.Message);
            return;
        }

        var pinout = AdapterPinout.Find(file.TestHardwareDeviceId);
        if (pinout is null)
        {
            await ShowErrorAsync(Localizer.Get("Error_OpenFixture"), Localizer.Format("Error_UnknownAdapter", file.TestHardwareDeviceId));
            return;
        }

        // The board may have moved together with the fixture or be somewhere else now: ask for it
        var boardPath = FixtureFile.ResolveStoredPath(file.BoardFile, path);
        if (!File.Exists(boardPath))
        {
            var choice = await AskAsync(Localizer.Get("Dialog_BoardMissingTitle"),
                Localizer.Format("Dialog_BoardMissing", boardPath),
                [new(Localizer.Get("Button_SelectBoard"), DialogResult.Yes, true), new(Localizer.Get("Button_Cancel"), DialogResult.Cancel)]);
            if (choice != DialogResult.Yes || Dialogs is null || await Dialogs.OpenBoardAsync() is not { } selected)
                return;
            boardPath = selected;
        }

        if (ReadBoard(boardPath) is not { } board)
            return;

        _isLoading = true;
        try
        {
            Probes.Clear();
            LoadPlates(file, path);
            Pinout = pinout;
            BuildPinLists();
            Board = board;
            FixturePath = path;
            _savedBoardHash = file.BoardHash;
            Name = file.Name;
            DeviceName = file.DeviceName;
            DeviceRevision = file.DeviceRevision;
            Author = file.Author;
            Notes = file.Notes;

            foreach (var entry in file.Probes.OrderBy(p => p.Number))
            {
                var probe = new Probe(entry.Reference, entry.Pad, entry.Side)
                {
                    Number = entry.Number,
                    Pin = pinout.FindFixturePin(entry.Pin),
                    Note = entry.Note,
                    Type = TypeOf(entry.ProbeType),
                    OpeningOverride = entry.Opening is > 0 ? entry.Opening : null,
                    SavedNet = entry.Net,
                    SavedPosition = entry.Net.Length > 0 || entry.X != 0 || entry.Y != 0 ? new Point2(entry.X, entry.Y) : null
                };
                Resolve(probe);
                Probes.Add(probe);
            }

            LoadWiring(file);
            SelectedPad = null;
            SelectedProbe = null;
            ResolveParts();
            BuildPadRows();
        }
        finally
        {
            _isLoading = false;
        }

        // A board that was selected instead of the stored one is a change
        IsDirty = !string.Equals(Path.GetFullPath(boardPath), Path.GetFullPath(FixtureFile.ResolveStoredPath(file.BoardFile, path)),
            StringComparison.OrdinalIgnoreCase);
        OnPropertyChanged(nameof(HasProbes));
        OnPropertyChanged(nameof(ProbeCountText));
        RunChecks();
        AfterFileChange();
        SetStatus(() => Localizer.Format("Status_FixtureOpened", Path.GetFileName(path), Probes.Count));
    }

    private Board? ReadBoard(string path)
    {
        try
        {
            return KicadBoardReader.Read(path);
        }
        catch (KicadFormatException ex)
        {
            _ = ShowErrorAsync(Localizer.Get("Error_OpenBoard"),
                Localizer.Format(ex.Error == KicadError.TooOld ? "Error_BoardTooOld" : "Error_NotABoard", Path.GetFileName(path)));
        }
        catch (FormatException ex)
        {
            _ = ShowErrorAsync(Localizer.Get("Error_OpenBoard"), Localizer.Format("Error_BoardDamaged", Path.GetFileName(path), ex.Message));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _ = ShowErrorAsync(Localizer.Get("Error_OpenBoard"), ex.Message);
        }

        return null;
    }

    /// <summary>Reads the board again, e.g. after it was changed in KiCad; the probes stay on their pads.</summary>
    [RelayCommand(CanExecute = nameof(CanEditProbes))]
    private void ReloadBoard() => ReplaceBoard(Board!.FilePath);

    /// <summary>Uses another board file for the fixture, e.g. a new revision of the board.</summary>
    [RelayCommand]
    private async Task ChangeBoard()
    {
        if (Board is null)
        {
            await New();
            return;
        }

        if (Dialogs is not null && await Dialogs.OpenBoardAsync() is { } path)
        {
            ReplaceBoard(path);
            MarkDirty();
        }
    }

    private void ReplaceBoard(string path)
    {
        if (ReadBoard(path) is not { } board)
            return;

        var selectedKey = SelectedPad?.Key;
        Board = board;
        foreach (var probe in Probes)
            Resolve(probe);
        ResolveParts();
        BuildPadRows();
        SelectedPad = selectedKey is null ? null : board.FindPad(selectedKey);
        RunChecks();
        var missing = Probes.Count(p => p.IsMissing);
        SetStatus(() => missing == 0
            ? Localizer.Format("Status_BoardReloaded", Path.GetFileName(path))
            : Localizer.Format("Status_BoardReloadedMissing", Path.GetFileName(path), missing));
    }

    private bool CanSave() => Board is not null;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task Save() => SaveAsync();

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveAs() => SaveAsAsync();

    /// <returns>False if the user canceled or saving failed.</returns>
    public async Task<bool> SaveAsync() => FixturePath is null ? await SaveAsAsync() : SaveTo(FixturePath);

    private async Task<bool> SaveAsAsync()
    {
        if (Dialogs is null)
            return false;

        var suggested = (Name.Length > 0 ? Name : Path.GetFileNameWithoutExtension(Board!.FilePath)) + FixtureFile.Extension;
        var path = await Dialogs.SaveFixtureAsync(SafeFileName(suggested));
        return path is not null && SaveTo(path);
    }

    private bool SaveTo(string path)
    {
        var board = Board!;
        var file = new FixtureFile
        {
            Name = Name,
            DeviceName = DeviceName,
            DeviceRevision = DeviceRevision,
            Author = Author,
            Notes = Notes,
            TestHardwareDeviceId = Pinout.TestHardware.DeviceId,
            BoardFile = FixtureFile.ToStoredPath(board.FilePath, path),
            BoardHash = board.Hash,
            Probes = Probes.OrderBy(p => p.Number).Select(p => new ProbeEntry
            {
                Number = p.Number,
                Reference = p.Reference,
                Pad = p.PadNumber,
                Side = p.Side,
                Pin = p.Pin?.Pin,
                Note = p.Note,
                ProbeType = p.Type?.Id,
                Opening = p.OpeningOverride,
                // A probe whose pad disappeared keeps what was known about it
                Net = p.Pad?.Net ?? p.SavedNet,
                X = p.Pad?.Position.X ?? p.SavedPosition?.X ?? 0,
                Y = p.Pad?.Position.Y ?? p.SavedPosition?.Y ?? 0
            }).ToList()
        };
        SavePlates(file, path);
        SaveWiring(file);

        try
        {
            file.Save(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _ = ShowErrorAsync(Localizer.Get("Error_SaveFixture"), ex.Message);
            return false;
        }

        // What is saved is the new reference for noticing board changes
        foreach (var probe in Probes.Where(p => p.Pad is not null))
        {
            probe.SavedNet = probe.Pad!.Net;
            probe.SavedPosition = probe.Pad.Position;
        }

        _savedBoardHash = board.Hash;
        FixturePath = path;
        IsDirty = false;
        RunChecks();
        AfterFileChange();
        SetStatus(() => Localizer.Format("Status_Saved", Path.GetFileName(path)));
        return true;
    }

    private void AfterFileChange()
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(FixtureFileName));
        OnPropertyChanged(nameof(BoardInfo));
    }

    private static string SafeFileName(string name) =>
        string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

    // ----- Build sheet -----

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task ExportBuildSheet()
    {
        if (Dialogs is null || Board is null)
            return;

        if (HasErrors)
        {
            var choice = await AskAsync(Localizer.Get("Dialog_ExportWithErrorsTitle"),
                Localizer.Format("Dialog_ExportWithErrors", ErrorCount),
                [new(Localizer.Get("Button_Export"), DialogResult.Yes, true), new(Localizer.Get("Button_Cancel"), DialogResult.Cancel)]);
            if (choice != DialogResult.Yes)
                return;
        }

        var baseName = FixturePath is not null ? Path.GetFileNameWithoutExtension(FixturePath) : Name.Length > 0 ? Name : "Fixture";
        var path = await Dialogs.SavePdfAsync(SafeFileName($"{baseName} - {Localizer.Get("Report_FileSuffix")}.pdf"));
        if (path is null)
            return;

        var data = new BuildSheetData
        {
            Name = Name,
            DeviceName = DeviceName,
            DeviceRevision = DeviceRevision,
            Author = Author,
            Notes = Notes,
            FixtureFile = FixturePath,
            Board = Board,
            Pinout = Pinout,
            Probes = Probes.OrderBy(p => p.Number).ToList(),
            Parts = Parts.ToList(),
            Components = Components.ToList(),
            Wires = Wires.Where(w => w.IsComplete).ToList(),
            PlateOutlines = Plate.PlatesOf(Probes, IsBoardFlipped, IsTopPlateCombined).ToDictionary(p => p, p => PlateOverlay(p)!),
            IsBoardFlipped = IsBoardFlipped,
            IsTopPlateCombined = IsTopPlateCombined,
            Layers = BuildSheetLayers.Effective
        };

        try
        {
            await Task.Run(() => BuildSheet.Generate(data, path));
            SetStatus(() => Localizer.Format("Status_BuildSheetSaved", Path.GetFileName(path)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await ShowErrorAsync(Localizer.Get("Error_BuildSheet"), ex.Message);
        }
    }

    // ----- Dialogs -----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDialogOpen))]
    private DialogViewModel? _dialog;

    public bool IsDialogOpen => Dialog is not null;

    private async Task<DialogResult> AskAsync(string title, string message, IReadOnlyList<DialogButton> buttons, bool isError = false)
    {
        // One at a time; a second request waits for the first
        while (Dialog is not null)
            await Dialog.Result;

        var dialog = new DialogViewModel(title, message, buttons, isError);
        Dialog = dialog;
        try
        {
            return await dialog.Result;
        }
        finally
        {
            Dialog = null;
        }
    }

    private Task<DialogResult> ShowErrorAsync(string title, string message) =>
        AskAsync(title, message, [new(Localizer.Get("Button_Ok"), DialogResult.Ok, true)], isError: true);

    /// <summary>Asks to save unsaved changes. False if the user canceled or saving failed.</summary>
    public async Task<bool> ConfirmDiscardAsync()
    {
        if (!IsDirty || Board is null)
            return true;

        var choice = await AskAsync(Localizer.Get("Dialog_UnsavedTitle"), Localizer.Get("Dialog_Unsaved"),
        [
            new(Localizer.Get("Button_Save"), DialogResult.Yes, true),
            new(Localizer.Get("Button_DontSave"), DialogResult.No),
            new(Localizer.Get("Button_Cancel"), DialogResult.Cancel)
        ]);

        return choice switch
        {
            DialogResult.Yes => await SaveAsync(),
            DialogResult.No => true,
            _ => false
        };
    }

    // ----- Theme -----

    [ObservableProperty] private bool _isDarkMode;

    partial void OnIsDarkModeChanged(bool value)
    {
        UpdateLayerColors(value);
        if (Application.Current is { } app)
            app.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;

        if (_settings is not null)
        {
            _settings.IsDarkMode = value;
            _settings.Save();
        }
    }

    [RelayCommand]
    private void ToggleTheme() => IsDarkMode = !IsDarkMode;

    // ----- About -----

    public AboutViewModel About { get; } = new();

    [ObservableProperty] private bool _isAboutOpen;

    [RelayCommand]
    private void ToggleAbout() => IsAboutOpen = !IsAboutOpen;

    [RelayCommand]
    private void CloseAbout() => IsAboutOpen = false;

    /// <summary>Escape: ends place mode, closes the dialog or the About box; with nothing of these open, clears the selection.</summary>
    [RelayCommand]
    private void Escape()
    {
        if (CancelPlacing())
            return;

        if (Dialog is not null)
        {
            Dialog.DismissCommand.Execute(null);
            return;
        }

        if (IsAboutOpen)
        {
            IsAboutOpen = false;
            return;
        }

        SelectedPart = null;
        SelectedProbe = null;
        SelectedPad = null;
    }

    /// <summary>True when a part was selected after the last probe, so Delete removes the part.</summary>
    private bool _isPartSelectedLast;

    private bool HasSelection() => SelectedPart is not null || SelectedProbe is not null;

    /// <summary>Delete: removes the selected part or probe, whichever was selected last.</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteSelected()
    {
        if (Dialog is not null || IsAboutOpen)
            return;

        if (SelectedPart is not null && (_isPartSelectedLast || SelectedProbe is null))
            RemovePart();
        else if (SelectedProbe is not null)
            RemoveProbe();
    }

    // ----- Language -----

    public IReadOnlyList<LanguageOption> Languages { get; }

    [ObservableProperty] private LanguageOption _selectedLanguage;

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        Localizer.Instance.ChangeCulture(value.Culture);
        if (_settings is not null)
        {
            _settings.Language = value.Culture.TwoLetterISOLanguageName;
            _settings.Save();
        }
    }

    /// <summary>Texts built in code follow the new language.</summary>
    private void OnCultureChanged()
    {
        foreach (var layer in LayerOptions)
            layer.RefreshText();
        BuildSheetLayers.RefreshDisplay();
        RunChecks();
        foreach (var row in _allPadRows)
            row.RefreshTexts();
        foreach (var row in PlateRows)
            row.RefreshTexts();
        foreach (var option in ColorOptions)
            option.RefreshText();
        RefreshOutlineTargets();
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(PadCountText));
        OnPropertyChanged(nameof(ProbeCountText));
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(FixtureFileName));
        OnPropertyChanged(nameof(FixturePathText));
        OnPropertyChanged(nameof(BoardInfo));
    }
}

/// <summary>Sorts "TP2" before "TP10" and pad "2" before "10".</summary>
public sealed class NaturalComparer : IComparer<string>
{
    public static NaturalComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (x is null || y is null)
            return string.Compare(x, y, StringComparison.Ordinal);

        int i = 0, j = 0;
        while (i < x.Length && j < y.Length)
        {
            if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
            {
                var si = i;
                var sj = j;
                while (i < x.Length && char.IsDigit(x[i])) i++;
                while (j < y.Length && char.IsDigit(y[j])) j++;
                var a = x[si..i].TrimStart('0');
                var b = y[sj..j].TrimStart('0');
                if (a.Length != b.Length)
                    return a.Length.CompareTo(b.Length);
                var c = string.CompareOrdinal(a, b);
                if (c != 0)
                    return c;
            }
            else
            {
                var c = char.ToUpperInvariant(x[i]).CompareTo(char.ToUpperInvariant(y[j]));
                if (c != 0)
                    return c;
                i++;
                j++;
            }
        }

        return (x.Length - i).CompareTo(y.Length - j);
    }
}