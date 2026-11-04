using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ATLab.FixtureDesigner.Fixture;
using ATLab.FixtureDesigner.Localization;
using ATLab.Pcb.Fixture;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.FixtureDesigner.ViewModels;

/// <summary>
/// Wiring besides a probe's own pin: components between the board and the test hardware (series resistors, relays …)
/// and wires between probes, pins and component terminals. Wiring only, the components get no place on a plate.
/// </summary>
public partial class MainWindowViewModel
{
    private readonly HashSet<FixtureComponent> _observedComponents = [];
    private readonly HashSet<Wire> _observedWires = [];

    /// <summary>The conductors as of the last check run, for the lists.</summary>
    private FixtureWiringModel _wiring = FixtureWiringModel.Empty;

    public ObservableCollection<FixtureComponent> Components { get; } = [];
    public ObservableCollection<Wire> Wires { get; } = [];

    /// <summary>Everything a wire can go to: the probes, the component terminals and the pins of the test hardware.</summary>
    public ObservableCollection<WireEndpoint> EndpointOptions { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveComponentCommand))]
    private FixtureComponent? _selectedComponent;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveWireCommand))]
    private Wire? _selectedWire;

    public bool HasComponents => Components.Count > 0;
    public bool HasWires => Wires.Count > 0;

    private void InitializeWiring()
    {
        Components.CollectionChanged += (_, _) =>
        {
            Sync(Components, _observedComponents, OnComponentChanged);
            OnPropertyChanged(nameof(HasComponents));
            BuildEndpointOptions();
        };
        Wires.CollectionChanged += (_, _) =>
        {
            Sync(Wires, _observedWires, OnWireChanged);
            for (var i = 0; i < Wires.Count; i++)
                Wires[i].Number = i + 1;
            OnPropertyChanged(nameof(HasWires));
        };
        Probes.CollectionChanged += (_, _) => BuildEndpointOptions();
    }

    private void BuildEndpointOptions()
    {
        var wanted = Probes.OrderBy(p => p.Number).Select(WireEndpoint.Of)
            .Concat(Components.SelectMany(c => c.Terminals.Select(t => WireEndpoint.Of(c, t))))
            .Concat(Pinout.FixturePins.Select(WireEndpoint.Of))
            .ToList();

        // Updated in place: entries that stay keep their place, so the wire lists keep their selection
        for (var i = 0; i < wanted.Count; i++)
        {
            if (i < EndpointOptions.Count && EndpointOptions[i] == wanted[i])
                continue;

            var existing = IndexOf(wanted[i], i + 1);
            if (existing >= 0)
                EndpointOptions.Move(existing, i);
            else
                EndpointOptions.Insert(i, wanted[i]);
        }

        while (EndpointOptions.Count > wanted.Count)
            EndpointOptions.RemoveAt(EndpointOptions.Count - 1);

        int IndexOf(WireEndpoint end, int start)
        {
            for (var j = start; j < EndpointOptions.Count; j++)
                if (EndpointOptions[j] == end)
                    return j;
            return -1;
        }
    }

    // ----- Components -----

    /// <summary>A new line to fill in: designator "?" and two terminals 1, 2.</summary>
    [RelayCommand(CanExecute = nameof(CanEditProbes))]
    private void AddComponent()
    {
        var component = new FixtureComponent { Terminals = ["1", "2"] };
        Components.Add(component);
        SelectedComponent = component;
        AfterWiringChanged();
    }

    private bool HasSelectedComponent() => SelectedComponent is not null;

    /// <summary>Removes the component and the wires to its terminals.</summary>
    [RelayCommand(CanExecute = nameof(HasSelectedComponent))]
    private void RemoveComponent()
    {
        if (SelectedComponent is not { } component)
            return;

        var index = Components.IndexOf(component);
        foreach (var wire in Wires.Where(w => w.Ends.Any(e => e.Terminal?.Component == component)).ToList())
            Wires.Remove(wire);
        Components.Remove(component);
        SelectedComponent = Components.Count == 0 ? null : Components[Math.Clamp(index, 0, Components.Count - 1)];
        AfterWiringChanged();
        SetStatus(() => Localizer.Format("Status_ProbeRemoved", component.Label));
    }

    private void OnComponentChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not FixtureComponent component)
            return;

        switch (e.PropertyName)
        {
            case nameof(FixtureComponent.Terminals):
                RenameTerminals(component);
                BuildEndpointOptions();
                AfterWiringChanged();
                break;
            case nameof(FixtureComponent.Designator):
                BuildEndpointOptions();
                AfterWiringChanged();
                break;
            case nameof(FixtureComponent.Description) or nameof(FixtureComponent.Note):
                MarkDirty();
                break;
        }
    }

    /// <summary>
    /// Wires to a terminal that was renamed follow it: a terminal that is gone is replaced by the one now in its place,
    /// e.g. 1, 2 typed over as A, K. Wires to terminals without a replacement stay and are reported by the checks.
    /// </summary>
    private void RenameTerminals(FixtureComponent component)
    {
        var old = component.PreviousTerminals;
        var now = component.Terminals;
        WireEndpoint? Renamed(WireEndpoint? end)
        {
            if (end?.Terminal is not { } terminal || terminal.Component != component || now.Contains(terminal.Terminal))
                return end;
            var index = old.ToList().IndexOf(terminal.Terminal);
            return index >= 0 && index < now.Count ? WireEndpoint.Of(component, now[index]) : end;
        }

        foreach (var wire in Wires)
        {
            wire.From = Renamed(wire.From);
            wire.To = Renamed(wire.To);
        }
    }

    // ----- Wires -----

    /// <summary>A new wire, starting at the selected probe if there is one.</summary>
    [RelayCommand(CanExecute = nameof(CanEditProbes))]
    private void AddWire()
    {
        var wire = new Wire { From = SelectedProbe is { } probe ? WireEndpoint.Of(probe) : null };
        Wires.Add(wire);
        SelectedWire = wire;
        AfterWiringChanged();
    }

    private bool HasSelectedWire() => SelectedWire is not null;

    [RelayCommand(CanExecute = nameof(HasSelectedWire))]
    private void RemoveWire()
    {
        if (SelectedWire is not { } wire)
            return;

        var index = Wires.IndexOf(wire);
        Wires.Remove(wire);
        SelectedWire = Wires.Count == 0 ? null : Wires[Math.Clamp(index, 0, Wires.Count - 1)];
        AfterWiringChanged();
    }

    private void OnWireChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Wire.From) or nameof(Wire.To))
            AfterWiringChanged();
        else if (e.PropertyName is nameof(Wire.Note))
            MarkDirty();
    }

    /// <summary>The wires of a removed probe go with it.</summary>
    private void RemoveWiresOf(Probe probe)
    {
        foreach (var wire in Wires.Where(w => w.Ends.Any(e => e.Probe == probe)).ToList())
            Wires.Remove(wire);
    }

    private void AfterWiringChanged()
    {
        MarkDirty();
        if (!_isBatchChange && !_isLoading)
            RunChecks();
    }

    /// <summary>What the wires join to each probe besides its pin, e.g. "R1.1, P5".</summary>
    private void UpdateWiredTo()
    {
        foreach (var probe in Probes)
        {
            var self = WireEndpoint.Of(probe);
            probe.WiredTo = string.Join(", ", Wires.Where(w => w.IsComplete && (w.From == self) != (w.To == self))
                .Select(w => w.From == self ? w.To!.Name : w.From!.Name).Distinct());
        }
    }

    // ----- File -----

    private void ResetWiring()
    {
        Wires.Clear();
        Components.Clear();
        SelectedWire = null;
        SelectedComponent = null;
        BuildEndpointOptions();
    }

    /// <summary>Components and wires of the file; the probes must be loaded first, the wires refer to them by number.</summary>
    private void LoadWiring(FixtureFile file)
    {
        ResetWiring();
        foreach (var entry in file.Components)
        {
            Components.Add(new FixtureComponent
            {
                Designator = entry.Designator,
                Description = entry.Description,
                Terminals = entry.Terminals.ToList(),
                Note = entry.Note
            });
        }

        var probes = Probes.GroupBy(p => p.Number).ToDictionary(g => g.Key, g => g.First());
        WireEndpoint? End(WireEnd end)
        {
            if (end.Probe is { } number)
                return probes.TryGetValue(number, out var probe) ? WireEndpoint.Of(probe) : null;
            if (Pinout.FindFixturePin(end.Pin) is { } pin)
                return WireEndpoint.Of(pin);
            if (end.Component is { } designator && end.Terminal is { } terminal &&
                Components.FirstOrDefault(c => string.Equals(c.Designator, designator, StringComparison.OrdinalIgnoreCase)) is { } component)
                return WireEndpoint.Of(component, terminal);
            return null;
        }

        foreach (var entry in file.Wires)
            Wires.Add(new Wire { From = End(entry.From), To = End(entry.To), Note = entry.Note });

        BuildEndpointOptions();
    }

    private void SaveWiring(FixtureFile file)
    {
        file.Components = Components.Select(c => new ComponentEntry
        {
            Designator = c.Designator,
            Description = c.Description,
            Terminals = c.Terminals.ToList(),
            Note = c.Note
        }).ToList();

        static WireEnd End(WireEndpoint? end) => new()
        {
            Probe = end?.Probe?.Number,
            Pin = end?.Pin?.Pin,
            Component = end?.Terminal?.Component.Designator,
            Terminal = end?.Terminal?.Terminal
        };

        file.Wires = Wires.Select(w => new WireEntry { From = End(w.From), To = End(w.To), Note = w.Note }).ToList();
    }
}
