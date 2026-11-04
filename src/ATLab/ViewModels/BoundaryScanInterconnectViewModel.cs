using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ATLab.BoundaryScan.Interconnect;
using ATLab.Enums;
using ATLab.Models;
using ATLab.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

/// <summary>A net of the interconnect test in the Boundary Scan tab.</summary>
public partial class InterconnectNetRowViewModel : ObservableObject
{
    private readonly BoundaryScanConfiguration _configuration;
    private readonly Action<Action> _edit;

    /// <param name="edit">Runs a change of the stored set-up without rebuilding the list, then checks the plan again.</param>
    public InterconnectNetRowViewModel(BoundaryScanConfiguration configuration, string net, InterconnectNetInfo? info, Action<Action> edit)
    {
        _configuration = configuration;
        _edit = edit;
        Net = net;
        Info = info;
        PinsText = info is null ? string.Empty : string.Join(", ", info.Pins.Select(p => p.DisplayName));
        OtherPadsText = info is null ? string.Empty : string.Join(", ", info.OtherPads);

        _role = configuration.InterconnectRoleOf(net);
        _connectedTo = configuration.FindInterconnectNet(net)?.ConnectedTo ?? string.Empty;
    }

    public string Net { get; }
    public InterconnectNetInfo? Info { get; }
    public string PinsText { get; }
    public string OtherPadsText { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RoleText), nameof(IsTested))]
    private InterconnectNetRole _role;

    public string RoleText => LocalizationService.Instance.GetString($"Enum_InterconnectNetRole_{Role}");
    public bool IsTested => Role != InterconnectNetRole.SAFE;

    /// <summary>Problems that keep the test from running, then hints, from the current plan.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNote))]
    private string _note = string.Empty;

    [ObservableProperty]
    private bool _hasProblem;

    public bool HasNote => Note.Length > 0;

    /// <summary>The net on the other side of a series resistor or similar; tested together with this one.</summary>
    [ObservableProperty]
    private string _connectedTo;

    partial void OnConnectedToChanged(string value)
    {
        if ((_configuration.FindInterconnectNet(Net)?.ConnectedTo ?? string.Empty) == value.Trim())
            return;

        _edit(() => _configuration.SetConnectedTo(Net, value));
    }

    /// <summary>Takes over the role from the stored set-up; connected nets change together.</summary>
    public void Refresh() => Role = _configuration.InterconnectRoleOf(Net);
}

/// <summary>
/// The interconnect test set-up: every net of the board with boundary scan pins and its role. All nets start in their
/// safe state; the user sets the roles of selected rows. The net list is built from the board and the BSDL files of
/// the chain devices that have a footprint reference.
/// </summary>
public partial class BoundaryScanInterconnectViewModel : ObservableObject
{
    private readonly BoundaryScanConfiguration _configuration;
    private readonly ObservableCollection<BoundaryScanDeviceViewModel> _devices;
    private readonly BoardService _boardService;
    private List<InterconnectNetRowViewModel> _allRows = [];
    private bool _isStale = true;
    private bool _isEditing;

    public BoundaryScanInterconnectViewModel(
        BoundaryScanConfiguration configuration,
        ObservableCollection<BoundaryScanDeviceViewModel> devices,
        BoardService boardService)
    {
        _configuration = configuration;
        _devices = devices;
        _boardService = boardService;

        _boardService.Changed += () => Dispatcher.UIThread.Post(Invalidate);

        // Devices, BSDL files and footprints change the net list; role changes made here do not
        _configuration.ConfigurationChanged += () =>
        {
            if (!_isEditing)
                Dispatcher.UIThread.Post(Invalidate);
        };
    }

    public ObservableCollection<InterconnectNetRowViewModel> Rows { get; } = new();

    /// <summary>Suggestions for the connected net: the nets of the list.</summary>
    public List<string> NetNames { get; private set; } = [];

    [ObservableProperty]
    private string _filter = string.Empty;

    [ObservableProperty]
    private bool _isTestedOnly;

    [ObservableProperty]
    private string _summary = string.Empty;

    /// <summary>Why there is no net list, e.g. no board or no device with footprint; empty if there is one.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMessage))]
    private string _message = string.Empty;

    public bool HasMessage => Message.Length > 0;

    [ObservableProperty]
    private bool _isVisible;

    partial void OnIsVisibleChanged(bool value)
    {
        if (value && _isStale)
            Rebuild();
    }

    partial void OnFilterChanged(string value) => ApplyFilter();
    partial void OnIsTestedOnlyChanged(bool value) => ApplyFilter();

    private void Invalidate()
    {
        _isStale = true;
        if (IsVisible)
            Rebuild();
    }

    [RelayCommand]
    private void Refresh() => Rebuild();

    [RelayCommand]
    private void SetSafe(IList? rows) => SetRole(rows, InterconnectNetRole.SAFE);

    [RelayCommand]
    private void SetDrive(IList? rows) => SetRole(rows, InterconnectNetRole.DRIVE);

    [RelayCommand]
    private void SetExpectHigh(IList? rows) => SetRole(rows, InterconnectNetRole.EXPECT_HIGH);

    [RelayCommand]
    private void SetExpectLow(IList? rows) => SetRole(rows, InterconnectNetRole.EXPECT_LOW);

    private void SetRole(IList? rows, InterconnectNetRole role)
    {
        var selected = rows?.OfType<InterconnectNetRowViewModel>().ToList() ?? [];
        if (selected.Count == 0)
            return;

        Edit(() =>
        {
            foreach (var row in selected)
                _configuration.SetInterconnectRole(row.Net, role);
        });
    }

    /// <summary>A change of the stored set-up made here: the list stays, the roles and the plan are updated.</summary>
    private void Edit(Action change)
    {
        _isEditing = true;
        try
        {
            change();
        }
        finally
        {
            _isEditing = false;
        }

        UpdatePlan();
    }

    private void Rebuild()
    {
        _isStale = false;
        Rows.Clear();
        _allRows = [];

        var devices = _devices.Select(d => new InterconnectDevice(d.Config, d.DisplayName, d.Bsdl)).ToList();
        var nets = _boardService.Board is { } board ? InterconnectNetlist.Build(board, devices) : [];

        var l = LocalizationService.Instance;
        Message = _boardService.Board is null ? l.GetString("Text_InterconnectNoBoard")
            : nets.Count == 0 ? l.GetString("Text_InterconnectNoNets")
            : string.Empty;

        _allRows = nets.Select(n => new InterconnectNetRowViewModel(_configuration, n.Net, n, Edit)).ToList();

        // Stored nets the board no longer has stay visible, so they can be reset
        foreach (var entry in _configuration.InterconnectNets.Where(e => nets.All(n => n.Net != e.Net)))
            _allRows.Add(new InterconnectNetRowViewModel(_configuration, entry.Net, null, Edit));

        NetNames = nets.Select(n => n.Net).ToList();
        OnPropertyChanged(nameof(NetNames));

        UpdatePlan();
        ApplyFilter();
    }

    /// <summary>Checks the set-up like the test does and shows the problems and hints in the rows.</summary>
    private void UpdatePlan()
    {
        var plan = InterconnectPlan.Create(_allRows.Where(r => r.Info != null).Select(r => r.Info!).ToList(), _configuration.InterconnectNets);

        var l = LocalizationService.Instance;
        foreach (var row in _allRows)
        {
            row.Refresh();

            // Connected nets share their problems and notes, filed under "A + B"
            var group = _configuration.ConnectedNets(row.Net);
            var key = plan.GroupName(row.Net);
            var problems = plan.Problems.GetValueOrDefault(key) ?? [];
            var notes = plan.Notes.GetValueOrDefault(key) ?? [];
            string[] together = group.Count > 1
                ? [string.Format(l.GetString("Text_InterconnectTestedTogether"), string.Join(" + ", group))]
                : [];
            row.Note = string.Join(" ", problems.Concat(together).Concat(notes));
            row.HasProblem = problems.Count > 0;
        }

        Summary = string.Format(l.GetString("Text_InterconnectSummary"),
            _allRows.Count(r => r.Info != null),
            _allRows.Count(r => r.Role == InterconnectNetRole.DRIVE),
            _allRows.Count(r => r.Role == InterconnectNetRole.EXPECT_HIGH),
            _allRows.Count(r => r.Role == InterconnectNetRole.EXPECT_LOW),
            plan.Problems.Count);

        if (IsTestedOnly)
            ApplyFilter();
    }

    private void ApplyFilter()
    {
        var terms = Filter.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var visible = _allRows.Where(r =>
            (!IsTestedOnly || r.IsTested) &&
            terms.All(t => r.Net.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                           r.PinsText.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                           r.OtherPadsText.Contains(t, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (visible.SequenceEqual(Rows))
            return;

        Rows.Clear();
        foreach (var row in visible)
            Rows.Add(row);
    }
}
