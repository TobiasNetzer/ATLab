using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using ATLab.Pcb.Controls;
using ATLab.Pcb.Fixture;
using ATLab.Pcb.Kicad;
using ATLab.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>
/// The KiCad board of the device under test and which net is on which pin of the adapter's DUT connector. Optional:
/// a project without board works as before. Filled from a fixture of ATLab Fixture Designer or by hand.
/// </summary>
public partial class BoardLink : ObservableObject
{
    public event Action? ConfigurationChanged;

    /// <summary>Path of the .kicad_pcb file; stored relative to the project file.</summary>
    [ObservableProperty]
    private string _boardFilePath = string.Empty;

    /// <summary>SHA-256 of the board file when the nets were assigned, to notice a changed board.</summary>
    [ObservableProperty]
    private string _boardHash = string.Empty;

    /// <summary>Fixture file the nets were imported from, for information; stored relative to the project file.</summary>
    [ObservableProperty]
    private string _fixtureFilePath = string.Empty;

    /// <summary>
    /// Whether the channel labels of the project are the nets of the channels instead of the labels entered by the
    /// user (those are kept and come back when this is turned off).
    /// </summary>
    [ObservableProperty]
    private bool _isNetNamesAsChannelLabels;

    /// <summary>Net of every pin of the DUT connector that has one.</summary>
    public ObservableCollection<PinNet> PinNets { get; set; } = new();

    public BoardLink()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not nameof(PinNets))
                OnConfigurationChanged();
        };
        PinNets.CollectionChanged += PinNetsChanged;
    }

    [JsonIgnore]
    public bool HasBoard => !string.IsNullOrWhiteSpace(BoardFilePath);

    partial void OnBoardFilePathChanged(string value) => OnPropertyChanged(nameof(HasBoard));

    /// <summary>Net of a pin of the DUT connector, e.g. "c7"; empty if none is assigned.</summary>
    public string NetOf(string pin) =>
        PinNets.FirstOrDefault(p => string.Equals(p.Pin, pin, StringComparison.OrdinalIgnoreCase))?.Net ?? string.Empty;

    /// <summary>
    /// Net of a relay channel: the first pin of the channel with a net, the terminals or polarities in the preferred
    /// order (e.g. "Out" before "In" for stimulation channels). Empty if none of its pins has a net.
    /// </summary>
    public string NetOfChannel(AdapterPinout pinout, ResourceKind kind, int channel, params string[] preferred) =>
        pinout.FixturePins.Where(p => p.Kind == kind && p.Channel == channel)
            .OrderBy(p => Array.IndexOf(preferred, p.Terminal ?? p.Polarity ?? string.Empty) is var i && i < 0 ? int.MaxValue : i)
            .Select(p => NetOf(p.Pin))
            .FirstOrDefault(n => n.Length > 0) ?? string.Empty;

    /// <summary>
    /// Channel labels from the nets: MEAS channels get their net, stimulation channels the net of Out (or In), external
    /// stimuli the net of H (or L), each shortened to the last part of the net path.
    /// </summary>
    public string ChannelLabel(AdapterPinout pinout, ResourceKind kind, int channel)
    {
        var net = kind switch
        {
            ResourceKind.Stim => NetOfChannel(pinout, kind, channel, "Out", "In"),
            ResourceKind.ExtStim => NetOfChannel(pinout, kind, channel, "H", "L"),
            _ => NetOfChannel(pinout, kind, channel)
        };
        return net.Length > 0 ? BoardTexts.ShortNet(net) : string.Empty;
    }

    public PinNet? Find(string pin) =>
        PinNets.FirstOrDefault(p => string.Equals(p.Pin, pin, StringComparison.OrdinalIgnoreCase));

    public void ResetToDefault() => CopyFrom(new BoardLink());

    public BoardLink Clone()
    {
        var clone = new BoardLink();
        clone.CopyFrom(this);
        return clone;
    }

    public void CopyFrom(BoardLink other)
    {
        BoardFilePath = other.BoardFilePath;
        BoardHash = other.BoardHash;
        FixtureFilePath = other.FixtureFilePath;
        IsNetNamesAsChannelLabels = other.IsNetNamesAsChannelLabels;

        PinNets.Clear();
        foreach (var pinNet in other.PinNets)
            PinNets.Add(pinNet.Clone());
    }

    /// <summary>Only assigned pins are written to the project file.</summary>
    public BoardLink ForSave(PathService? pathService)
    {
        var clone = new BoardLink
        {
            BoardFilePath = pathService?.ToRelative(BoardFilePath) ?? BoardFilePath,
            BoardHash = BoardHash,
            FixtureFilePath = pathService?.ToRelative(FixtureFilePath) ?? FixtureFilePath,
            IsNetNamesAsChannelLabels = IsNetNamesAsChannelLabels
        };
        foreach (var pinNet in PinNets.Where(p => p.Net.Length > 0 || p.Probes.Count > 0))
            clone.PinNets.Add(pinNet.Clone());
        return clone;
    }

    public void RestoreAfterLoad(PathService pathService)
    {
        if (!string.IsNullOrWhiteSpace(BoardFilePath))
            BoardFilePath = pathService.ToAbsolute(BoardFilePath);
        if (!string.IsNullOrWhiteSpace(FixtureFilePath))
            FixtureFilePath = pathService.ToAbsolute(FixtureFilePath);
    }

    private void PinNetsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var pinNet in e.OldItems?.OfType<PinNet>() ?? [])
            pinNet.PropertyChanged -= PinNetChanged;
        foreach (var pinNet in e.NewItems?.OfType<PinNet>() ?? [])
            pinNet.PropertyChanged += PinNetChanged;

        OnConfigurationChanged();
    }

    private void PinNetChanged(object? sender, PropertyChangedEventArgs e) => OnConfigurationChanged();

    private void OnConfigurationChanged() => ConfigurationChanged?.Invoke();
}

/// <summary>Net on a pin of the adapter's DUT connector, and the probes of the fixture that touch it.</summary>
public partial class PinNet : ObservableObject
{
    /// <summary>Pin of the DUT connector, e.g. "c7".</summary>
    public string Pin { get; set; } = string.Empty;

    [ObservableProperty]
    private string _net = string.Empty;

    /// <summary>Probes from the fixture file, shown on the board; empty for nets assigned by hand.</summary>
    public List<ProbePoint> Probes { get; set; } = new();

    public PinNet Clone() => new()
    {
        Pin = Pin,
        Net = Net,
        Probes = Probes.Select(p => p with { }).ToList()
    };
}

/// <summary>
/// A probe of the fixture: its name and the pad it touches from which side. X and Y are the position of the pad on the
/// board, which tells apart several pads with one number; null in projects imported before they were kept.
/// </summary>
public sealed record ProbePoint(string Name, string Reference, string Pad, BoardSide Side, double? X = null, double? Y = null);