using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATLab.BoundaryScan.Bsdl;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Pcb.Controls;
using ATLab.Pcb.Fixture;
using ATLab.Pcb.Kicad;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Services;

/// <summary>What a step is shown with on the board: role, adapter resource and net, in the color of the role.</summary>
public sealed record BoardLegendItem(string Role, string Resource, string Net, Color Color)
{
    public IBrush Brush => new SolidColorBrush(Color);
    public bool HasNet => Net.Length > 0;
}

/// <summary>Everything a test step touches on the board.</summary>
public sealed record StepHighlight(
    IReadOnlyList<NetHighlight> Nets,
    IReadOnlyList<PadHighlight> Pads,
    IReadOnlyList<FootprintHighlight> Footprints,
    IReadOnlyList<IBoardMarker> Probes,
    IReadOnlyList<BoardLegendItem> Legend)
{
    public static StepHighlight Empty { get; } = new([], [], [], [], []);

    public bool IsEmpty => Nets.Count == 0 && Pads.Count == 0 && Footprints.Count == 0;
}

/// <summary>Which nets of the board the steps of the project measure, stimulate or communicate with.</summary>
public sealed record BoardCoverage(IReadOnlySet<string> CoveredNets, int TotalNets);

/// <summary>What a connection of a step is on the board; each can be hidden in the view and has its color.</summary>
public enum BoardRole
{
    High,
    Low,
    Stimulus,
    ExternalStimulus,
    Interface,
    BoundaryScan,
    Failed
}

/// <summary>
/// Colors of the roles on the board, by default H and L like the leads of a multimeter. The user can choose others;
/// they are used by the board view and the board pictures of the PDF report.
/// </summary>
public static class BoardColors
{
    private static readonly Dictionary<BoardRole, Color> Defaults = new()
    {
        [BoardRole.High] = Color.Parse("#EF4444"),
        [BoardRole.Low] = Color.Parse("#3B82F6"),
        [BoardRole.Stimulus] = Color.Parse("#22C55E"),
        [BoardRole.ExternalStimulus] = Color.Parse("#F59E0B"),
        [BoardRole.Interface] = Color.Parse("#A855F7"),
        [BoardRole.BoundaryScan] = Color.Parse("#06B6D4"),
        [BoardRole.Failed] = Color.Parse("#DC2626")
    };

    private static readonly Dictionary<BoardRole, Color> Current = new(Defaults);

    public static Color Of(BoardRole role) => Current[role];
    public static Color DefaultOf(BoardRole role) => Defaults[role];
    public static void Set(BoardRole role, Color color) => Current[role] = color;

    /// <summary>Takes over the colors the user chose (role name → "#RRGGBB"); unknown or invalid entries are ignored.</summary>
    public static void Load(IReadOnlyDictionary<string, string>? colors)
    {
        foreach (var role in Enum.GetValues<BoardRole>())
            Current[role] = colors is not null && colors.TryGetValue(role.ToString(), out var hex) && Color.TryParse(hex, out var color)
                ? color
                : Defaults[role];
    }

    public static Color High => Of(BoardRole.High);
    public static Color Low => Of(BoardRole.Low);
    public static Color Stimulus => Of(BoardRole.Stimulus);
    public static Color ExternalStimulus => Of(BoardRole.ExternalStimulus);
    public static Color Interface => Of(BoardRole.Interface);
    public static Color BoundaryScan => Of(BoardRole.BoundaryScan);
    public static Color Failed => Of(BoardRole.Failed);
    public static readonly Color Covered = Color.Parse("#22C55E");
}

/// <summary>
/// The KiCad board of the open project and what the test steps touch on it. Loads the board when the project or its
/// board file changes and notices when the file changed since the nets were assigned.
/// </summary>
public partial class BoardService : ObservableObject
{
    private readonly ProjectModel _project;
    private readonly ILoggingService _loggingService;
    private readonly Dictionary<string, (DateTime Modified, BsdlDevice? Device)> _bsdlCache = new(StringComparer.OrdinalIgnoreCase);
    private string _loadedPath = string.Empty;
    private int _loadVersion;

    public BoardService(ProjectModel project, ILoggingService loggingService, ISettingsService settingsService)
    {
        _project = project;
        _loggingService = loggingService;
        BoardColors.Load(settingsService.Settings.BoardRoleColors);

        _project.Board.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(BoardLink.BoardFilePath))
                _ = LoadAsync();
            else if (e.PropertyName == nameof(BoardLink.BoardHash))
                UpdateChangeState();
        };
    }

    /// <summary>Raised after a board was loaded or removed, and when the nets of the pins change.</summary>
    public event Action? Changed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBoard))]
    private Board? _board;

    public bool HasBoard => Board is not null;

    [ObservableProperty] private bool _isLoading;

    /// <summary>Why the board could not be loaded; empty if it was, or if the project has none.</summary>
    [ObservableProperty] private string _loadError = string.Empty;

    /// <summary>The board file is not the one the nets were assigned for (checksum differs).</summary>
    [ObservableProperty] private bool _isBoardChanged;

    /// <summary>Pinout of the project's test hardware; null for hardware without one.</summary>
    public AdapterPinout? Pinout => AdapterPinout.Find(TestHardwareIdFormat.Format(_project.TargetTestHardwareId));

    /// <summary>Reads the board file of the project again, e.g. after a project was opened.</summary>
    public async Task LoadAsync()
    {
        var path = _project.Board.BoardFilePath;
        var version = ++_loadVersion;

        if (string.IsNullOrWhiteSpace(path))
        {
            _loadedPath = string.Empty;
            Board = null;
            LoadError = string.Empty;
            IsBoardChanged = false;
            Changed?.Invoke();
            return;
        }

        IsLoading = true;
        try
        {
            var board = await Task.Run(() => KicadBoardReader.Read(path));
            if (version != _loadVersion)
                return;

            _loadedPath = path;
            Board = board;
            LoadError = string.Empty;

            // A board set before its checksum was stored is the reference from now on
            if (string.IsNullOrEmpty(_project.Board.BoardHash))
            {
                using (_project.SuppressDirtyTracking())
                    _project.Board.BoardHash = board.Hash;
            }

            UpdateChangeState();
            if (IsBoardChanged)
                _loggingService.Warning(LocalizationService.Instance.GetString("Log_BoardChanged")
                    .Replace("{0}", Path.GetFileName(path)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException or KicadFormatException)
        {
            if (version != _loadVersion)
                return;

            Board = null;
            LoadError = ex is KicadFormatException { Error: KicadError.TooOld }
                ? LocalizationService.Instance.GetString("Board_ErrorTooOld")
                : ex is FileNotFoundException or DirectoryNotFoundException
                    ? LocalizationService.Instance.GetString("Board_ErrorNotFound")
                    : ex.Message;
            IsBoardChanged = false;
            _loggingService.Warning($"{LocalizationService.Instance.GetString("Log_BoardNotLoaded")} {Path.GetFileName(path)}: {LoadError}");
        }
        finally
        {
            if (version == _loadVersion)
                IsLoading = false;
        }

        Changed?.Invoke();
    }

    private void UpdateChangeState() =>
        IsBoardChanged = Board is not null && _project.Board.BoardHash.Length > 0 &&
                         !string.Equals(_project.Board.BoardHash, Board.Hash, StringComparison.OrdinalIgnoreCase);

    /// <summary>The current board file becomes the reference: the nets were checked against it.</summary>
    public void AcceptBoardChange()
    {
        if (Board is null)
            return;

        _project.Board.BoardHash = Board.Hash;
        UpdateChangeState();
    }

    /// <summary>Tells the views that the nets of the pins changed.</summary>
    public void NotifyNetsChanged() => Changed?.Invoke();

    // ----- Highlights of a step -----

    /// <summary>
    /// The nets a step connects to through the matrix (H, L), the stimulation relays and the interfaces, the pins of
    /// boundary scan steps, and the probes of the fixture on them. Hidden roles are left out, also from the legend.
    /// </summary>
    public StepHighlight GetHighlight(TestStep? step, IReadOnlySet<BoardRole>? hidden = null)
    {
        if (step is null || Pinout is not { } pinout)
            return StepHighlight.Empty;

        var nets = new List<NetHighlight>();
        var pads = new List<PadHighlight>();
        var footprints = new List<FootprintHighlight>();
        var probes = new List<IBoardMarker>();
        var legend = new List<BoardLegendItem>();
        var l = LocalizationService.Instance;
        bool Shown(BoardRole role) => hidden is null || !hidden.Contains(role);

        // Only H and L need a role in the legend; the resource names the others (STIM 7 Out, I²C SDA …)
        void AddPin(AdapterPin? pin, string role, BoardRole category)
        {
            if (pin is null || !Shown(category))
                return;

            var color = BoardColors.Of(category);

            var pinNet = _project.Board.Find(pin.Pin);
            var net = pinNet?.Net ?? string.Empty;
            legend.Add(new BoardLegendItem(role, pin.Resource, net.Length > 0 ? BoardTexts.ShortNet(net) : string.Empty, color));

            if (net.Length > 0 && nets.All(n => n.Net != net))
                nets.Add(new NetHighlight(net, color));

            foreach (var probe in pinNet?.Probes ?? [])
                if (Board?.FindPad(Pad.PadKeyOf(probe.Reference, probe.Pad)) is { } pad)
                    probes.Add(new StepProbeMarker(probe, pad, color));
        }

        // Measurement bus
        var matrix = step.MatrixState;
        if (matrix.IsExternalProbe)
        {
            if (Shown(BoardRole.High))
                legend.Add(new BoardLegendItem("H", l.GetString("Board_ExternalProbe"), string.Empty, BoardColors.High));
        }
        else if (matrix.ActiveChannelHigh > 0)
            AddPin(pinout.FixturePins.FirstOrDefault(p => p.Kind == ResourceKind.Meas && p.Channel == matrix.ActiveChannelHigh), "H", BoardRole.High);
        if (matrix.ActiveChannelLow > 0)
            AddPin(pinout.FixturePins.FirstOrDefault(p => p.Kind == ResourceKind.Meas && p.Channel == matrix.ActiveChannelLow), "L", BoardRole.Low);

        // Closed stimulation relays connect In and Out
        foreach (var channel in step.LiveStimState.Channels.Where(c => c.IsEnabled))
            foreach (var pin in pinout.FixturePins.Where(p => p.Kind == ResourceKind.Stim && p.Channel == channel.ChannelIndex))
                AddPin(pin, string.Empty, BoardRole.Stimulus);

        foreach (var channel in step.LiveExtStimState.Channels.Where(c => c.IsEnabled))
            foreach (var pin in pinout.FixturePins.Where(p => p.Kind == ResourceKind.ExtStim && p.Channel == channel.ChannelIndex))
                AddPin(pin, string.Empty, BoardRole.ExternalStimulus);

        // Interface command: all lines of the interface
        if (step.EvaluationSource == TestEvaluationSource.INTERFACE)
        {
            var name = step.InterfaceConfig.InterfaceType switch
            {
                CommunicationInterfaceType.I2C => "I2C",
                CommunicationInterfaceType.UART => "UART",
                _ => "RS485"
            };
            // All lines including VREF and GND; their planes only show with the plane option of the view
            foreach (var pin in pinout.FixturePins.Where(p => p.Kind == ResourceKind.Interface && p.Interface == name))
                AddPin(pin, string.Empty, BoardRole.Interface);
        }

        // Boundary scan: the pins of the devices on their footprints
        if (!Shown(BoardRole.BoundaryScan))
        {
            // Pins and chip of boundary scan steps hidden
        }
        else if (step.EvaluationSource == TestEvaluationSource.BOUNDARY_SCAN)
        {
            foreach (var action in step.BoundaryScan.PinActions)
                AddScanPin(action.DeviceId, action.Port, null, pads, nets, footprints);
        }
        else if (step.EvaluationSource == TestEvaluationSource.BOUNDARY_SCAN_BUS &&
                 _project.BoundaryScan.Buses.FirstOrDefault(b => b.Id == step.BoundaryScanBus.BusId) is { } bus)
        {
            foreach (var pin in bus.Pins.Where(p => p.IsAssigned))
                AddScanPin(pin.DeviceId, pin.Port, pin.SignalName, pads, nets, footprints);
        }
        else if (step.EvaluationSource == TestEvaluationSource.BOUNDARY_SCAN_INTERCONNECT)
        {
            // The nets the interconnect test drives or checks; nets in their safe state are not touched
            var tested = _project.BoundaryScan.InterconnectNets.Where(n => n.Role != InterconnectNetRole.SAFE).Select(n => n.Net).ToList();
            foreach (var net in tested.Where(n => nets.All(h => h.Net != n)))
                nets.Add(new NetHighlight(net, BoardColors.BoundaryScan));

            if (tested.Count > 0)
                legend.Add(new BoardLegendItem(string.Empty, l.GetString("Board_InterconnectLegend"),
                    l.GetString("Board_InterconnectNets").Replace("{0}", tested.Count.ToString()), BoardColors.BoundaryScan));
        }

        foreach (var footprint in footprints)
        {
            var count = pads.Count(p => p.Pad.Footprint.Reference == footprint.Reference);
            legend.Add(new BoardLegendItem(string.Empty, footprint.Reference, LocalizationService.Instance.GetString("Board_ScanPins").Replace("{0}", count.ToString()), BoardColors.BoundaryScan));
        }

        return new StepHighlight(nets, pads, footprints, probes, legend);
    }

    /// <summary>
    /// A boundary scan pin: the device's footprint (set in the Boundary Scan tab), the package pin of the port from
    /// the BSDL file, and that pad of the footprint with its net.
    /// </summary>
    private void AddScanPin(string deviceId, string port, string? label, List<PadHighlight> pads, List<NetHighlight> nets,
        List<FootprintHighlight> footprints)
    {
        if (Board is not { } board)
            return;

        var device = _project.BoundaryScan.Devices.FirstOrDefault(d => d.Id == deviceId);
        if (device is null || string.IsNullOrWhiteSpace(device.FootprintReference))
            return;

        var reference = device.FootprintReference.Trim();
        if (footprints.All(f => f.Reference != reference) && board.Footprints.Any(f => f.Reference == reference))
            footprints.Add(new FootprintHighlight(reference, BoardColors.BoundaryScan));

        if (Bsdl(device.BsdlFilePath) is not { } bsdl)
            return;

        var package = !string.IsNullOrWhiteSpace(device.PackageName) && bsdl.Packages.ContainsKey(device.PackageName)
            ? device.PackageName
            : bsdl.DefaultPackage;
        if (!bsdl.Packages.TryGetValue(package, out var pins))
            return;

        foreach (var pin in pins.Where(p => string.Equals(p.Port, port, StringComparison.OrdinalIgnoreCase)))
        {
            if (board.FindPad(Pad.PadKeyOf(reference, pin.PinName)) is not { } pad || pads.Any(p => p.Pad == pad))
                continue;

            var pinLabel = device.GetPinLabel(port);
            var text = label ?? (pinLabel.Length > 0 ? pinLabel : port);
            pads.Add(new PadHighlight(pad, BoardColors.BoundaryScan, text));
            if (pad.HasNet && nets.All(n => n.Net != pad.Net))
                nets.Add(new NetHighlight(pad.Net, BoardColors.BoundaryScan));
        }
    }

    /// <summary>BSDL files are read once and again when they change.</summary>
    private BsdlDevice? Bsdl(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        var modified = File.GetLastWriteTimeUtc(path);
        if (_bsdlCache.TryGetValue(path, out var cached) && cached.Modified == modified)
            return cached.Device;

        var result = BsdlParser.ParseFile(path);
        var device = result.IsSuccess ? result.Value : null;
        _bsdlCache[path] = (modified, device);
        return device;
    }

    // ----- Coverage -----

    /// <summary>The nets the steps of the project reach; steps marked as ignored do not count.</summary>
    public BoardCoverage GetCoverage(IEnumerable<TestStep> steps, IReadOnlySet<BoardRole>? hidden = null)
    {
        // Only nets of the board count, so a net typed by hand that the board lacks cannot raise the share
        var boardNets = Board?.Nets.ToHashSet(StringComparer.Ordinal);
        var covered = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in steps.Where(s => !s.IsIgnoreStep))
            foreach (var net in GetHighlight(step, hidden).Nets)
                if (boardNets is null || boardNets.Contains(net.Net))
                    covered.Add(net.Net);

        return new BoardCoverage(covered, Board?.Nets.Count ?? 0);
    }
}

/// <summary>A probe of the fixture on a pin a step uses, in the color of its role.</summary>
public sealed class StepProbeMarker(ProbePoint probe, Pad pad, Color color) : IBoardMarker
{
    public event PropertyChangedEventHandler? PropertyChanged
    {
        add { }
        remove { }
    }

    public BoardMarkerKind MarkerKind => BoardMarkerKind.Probe;
    public Point2? MarkerPosition => pad.Position;
    public double MarkerSize => pad.MinSize;
    public BoardSide? MarkerSide => probe.Side;
    public string MarkerLabel => probe.Name;
    public BoardMarkerState MarkerState => BoardMarkerState.Normal;
    public Pad? MarkerPad => pad;
    public Color? MarkerColor => color;
}