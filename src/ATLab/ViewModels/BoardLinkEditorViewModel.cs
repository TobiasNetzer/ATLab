using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Pcb.Controls;
using ATLab.Pcb.Fixture;
using ATLab.Services;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

/// <summary>
/// Configuration of the board view: the KiCad board of the device under test and the net on every pin of the
/// adapter's DUT connector, imported from a fixture of ATLab Fixture Designer or assigned by hand.
/// </summary>
public partial class BoardLinkEditorViewModel : ObservableObject
{
    private readonly ProjectModel _project;
    private readonly BoardService _boardService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly ILoggingService _loggingService;

    public BoardLinkEditorViewModel(ProjectModel project, BoardService boardService, IFileDialogService fileDialogService,
        IMessageBoxService messageBoxService, ILoggingService loggingService)
    {
        _project = project;
        _boardService = boardService;
        _fileDialogService = fileDialogService;
        _messageBoxService = messageBoxService;
        _loggingService = loggingService;

        boardService.Changed += () => Dispatcher.UIThread.Post(() => Rebuild(onlyIfChanged: true));
        boardService.PropertyChanged += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IsBoardChanged));
        });
        project.Board.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(BoardLink.BoardFilePath) or nameof(BoardLink.FixtureFilePath))
            {
                OnPropertyChanged(nameof(BoardFilePath));
                OnPropertyChanged(nameof(FixtureFileName));
                OnPropertyChanged(nameof(HasBoardFile));
            }
        };
        // Another project: other pins and nets
        project.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ProjectModel.FilePath))
                Dispatcher.UIThread.Post(Rebuild);
        };

        Rebuild();
    }

    public string BoardFilePath => _project.Board.BoardFilePath;
    public bool HasBoardFile => _project.Board.HasBoard;

    public string FixtureFileName => string.IsNullOrEmpty(_project.Board.FixtureFilePath)
        ? string.Empty
        : Path.GetFileName(_project.Board.FixtureFilePath);

    public bool IsBoardChanged => _boardService.IsBoardChanged;

    /// <summary>Loaded board, or why it is missing.</summary>
    public string Status
    {
        get
        {
            var l = LocalizationService.Instance;
            if (!_project.Board.HasBoard)
                return l.GetString("BoardLink_NoBoard");
            if (_boardService.IsLoading)
                return l.GetString("Board_Loading");
            if (_boardService.LoadError.Length > 0)
                return l.GetString("Board_LoadFailed").Replace("{0}", _boardService.LoadError);
            if (_boardService.Board is { } board)
            {
                var text = l.GetString("BoardLink_Loaded")
                    .Replace("{0}", Path.GetFileName(board.FilePath))
                    .Replace("{1}", board.Footprints.Count.ToString())
                    .Replace("{2}", board.Nets.Count.ToString());
                return IsBoardChanged ? text + " " + l.GetString("Board_ChangedNotice") : text;
            }

            return string.Empty;
        }
    }

    /// <summary>Nets of the board for the suggestions while typing.</summary>
    [ObservableProperty] private IReadOnlyList<string> _netNames = [];

    /// <summary>One row per pin of the DUT connector.</summary>
    [ObservableProperty] private IReadOnlyList<PinNetRow> _rows = [];

    [ObservableProperty] private bool _assignedOnly;

    partial void OnAssignedOnlyChanged(bool value) => Rebuild();

    public string AssignedText =>
        LocalizationService.Instance.GetString("BoardLink_Assigned")
            .Replace("{0}", _project.Board.PinNets.Count(p => p.Net.Length > 0).ToString())
            .Replace("{1}", (_boardService.Pinout?.FixturePins.Count ?? 0).ToString());

    // What the rows were built for: a net edited in a row does not need new rows
    private (object? Board, object? Pinout, bool AssignedOnly) _rowsKey;

    private void Rebuild() => Rebuild(onlyIfChanged: false);

    /// <summary>
    /// New rows for the pins. After a net was edited (onlyIfChanged) the rows stay: recreating them would take the
    /// edited box away, e.g. while a suggestion of its list is being picked.
    /// </summary>
    private void Rebuild(bool onlyIfChanged)
    {
        NetNames = _boardService.Board?.Nets ?? [];
        var pinout = _boardService.Pinout;
        var key = ((object?)_boardService.Board, (object?)pinout, AssignedOnly);
        if (!onlyIfChanged || key != _rowsKey)
        {
            _rowsKey = key;
            BuildRows(pinout);
        }

        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(IsBoardChanged));
        OnPropertyChanged(nameof(AssignedText));
    }

    private void BuildRows(AdapterPinout? pinout)
    {
        var pins = pinout?.FixturePins ?? [];
        Rows = pins
            .Select(p => new PinNetRow(p, _project.Board, this))
            .Where(r => !AssignedOnly || r.Net.Length > 0)
            .ToList();
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(IsBoardChanged));
        OnPropertyChanged(nameof(AssignedText));
    }

    internal void OnNetEdited()
    {
        OnPropertyChanged(nameof(AssignedText));
        _boardService.NotifyNetsChanged();
    }

    // ----- Board file -----

    [RelayCommand]
    private async Task BrowseBoard()
    {
        var file = await _fileDialogService.OpenFileAsync(LocalizationService.Instance.GetString("BoardLink_SelectBoard"), ["kicad_pcb"]);
        if (file?.TryGetLocalPath() is not { } path)
            return;

        // A new board file: its checksum becomes the reference once it is loaded
        _project.Board.BoardHash = string.Empty;
        _project.Board.BoardFilePath = path;
    }

    [RelayCommand]
    private void RemoveBoard()
    {
        _project.Board.BoardFilePath = string.Empty;
        _project.Board.BoardHash = string.Empty;
    }

    [RelayCommand]
    private Task ReloadBoard() => _boardService.LoadAsync();

    [RelayCommand]
    private void AcceptBoardChange() => _boardService.AcceptBoardChange();

    [RelayCommand]
    private async Task ClearNets()
    {
        var l = LocalizationService.Instance;
        if (!await _messageBoxService.ShowConfirmationDestructiveAsync(l.GetString("BoardLink_ClearTitle"), l.GetString("BoardLink_ClearMessage")))
            return;

        _project.Board.PinNets.Clear();
        _project.Board.FixtureFilePath = string.Empty;
        Rebuild();
        _boardService.NotifyNetsChanged();
    }

    // ----- Fixture import -----

    /// <summary>
    /// Takes over the board, the nets of the wired pins and the probe positions from a fixture of ATLab Fixture
    /// Designer; on request also the channel labels.
    /// </summary>
    [RelayCommand]
    private async Task ImportFixture()
    {
        var l = LocalizationService.Instance;
        var file = await _fileDialogService.OpenFileAsync(l.GetString("BoardLink_SelectFixture"), [FixtureFile.Extension.TrimStart('.')]);
        if (file?.TryGetLocalPath() is not { } fixturePath)
            return;

        FixtureFile fixture;
        try
        {
            fixture = FixtureFile.Load(fixturePath);
        }
        catch (Exception ex) when (ex is FixtureFileException or IOException or UnauthorizedAccessException)
        {
            await _messageBoxService.ShowMessageAsync(l.GetString("BoardLink_ImportFailed"),
                ex is FixtureFileException ? l.GetString("BoardLink_NotAFixture") : ex.Message);
            return;
        }

        // Pins wired to probes get their net; a pin behind a component of the fixture is on another net and gets none
        var pinNets = FixtureWiring.PinNets(fixture)
            .Select(p => new PinNet
            {
                Pin = p.Pin,
                Net = p.Net,
                Probes = p.Probes.Select(e => new ProbePoint($"P{e.Number}", e.Reference, e.Pad, e.Side)).ToList()
            })
            .ToList();

        _project.Board.PinNets.Clear();
        foreach (var pinNet in pinNets)
            _project.Board.PinNets.Add(pinNet);

        // The board of the fixture, with the checksum it had when the fixture was saved
        _project.Board.FixtureFilePath = fixturePath;
        _project.Board.BoardHash = fixture.BoardHash;
        var boardPath = FixtureFile.ResolveStoredPath(fixture.BoardFile, fixturePath);
        if (_project.Board.BoardFilePath == boardPath)
            await _boardService.LoadAsync();
        else
            _project.Board.BoardFilePath = boardPath;

        // Not asked while the project shows the net names as labels anyway
        if (!_project.Board.IsNetNamesAsChannelLabels &&
            await _messageBoxService.ShowConfirmationImageAsync(l.GetString("BoardLink_LabelsTitle"),
                l.GetString("BoardLink_LabelsMessage"), string.Empty))
            ApplyChannelLabels();

        Rebuild();
        _boardService.NotifyNetsChanged();
        _loggingService.Info(l.GetString("BoardLink_Imported")
            .Replace("{0}", Path.GetFileName(fixturePath))
            .Replace("{1}", pinNets.Count(p => p.Net.Length > 0).ToString()));
    }

    /// <summary>
    /// Channel labels from the nets: MEAS channels get their net, stimulation channels the net of Out (or In), external
    /// stimuli the net of H (or L). Channels without net keep their label.
    /// </summary>
    private void ApplyChannelLabels()
    {
        if (_boardService.Pinout is not { } pinout)
            return;

        void Apply(IEnumerable<CustomRelayChannelName> channels, ResourceKind kind)
        {
            foreach (var channel in channels.Where(c => c.ChannelIndex is not null))
                if (_project.Board.ChannelLabel(pinout, kind, channel.ChannelIndex!.Value) is { Length: > 0 } label)
                    channel.ChannelName = label;
        }

        Apply(_project.MeasChannelNames, ResourceKind.Meas);
        Apply(_project.StimChannelNames, ResourceKind.Stim);
        Apply(_project.ExtStimChannelNames, ResourceKind.ExtStim);
    }
}

/// <summary>A pin of the DUT connector with its resource and net in the board configuration.</summary>
public sealed class PinNetRow(AdapterPin pin, BoardLink board, BoardLinkEditorViewModel owner)
{
    public string Pin => pin.Pin;
    public string Resource => pin.Resource;
    public string Signal => pin.Signal;

    /// <summary>Probes of the fixture on this pin, e.g. "P3, P7".</summary>
    public string Probes => string.Join(", ", board.Find(pin.Pin)?.Probes.Select(p => p.Name) ?? []);

    /// <summary>Net of the pin; a pin gets its entry when a net is assigned, an empty net removes it.</summary>
    public string Net
    {
        get => board.NetOf(pin.Pin);
        set
        {
            var net = value?.Trim() ?? string.Empty;
            var entry = board.Find(pin.Pin);
            if (entry is null)
            {
                if (net.Length == 0)
                    return;
                board.PinNets.Add(new PinNet { Pin = pin.Pin, Net = net });
            }
            else if (net.Length == 0 && entry.Probes.Count == 0)
            {
                board.PinNets.Remove(entry);
            }
            else
            {
                entry.Net = net;
            }

            owner.OnNetEdited();
        }
    }
}