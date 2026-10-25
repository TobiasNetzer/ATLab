using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using ATLab.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

/// <summary>
/// Boundary scan settings of a test step. Pins that are not listed keep the state of the previous steps.
/// </summary>
public partial class BoundaryScanStepConfig : ObservableObject
{
    [ObservableProperty]
    private BoundaryScanStepMode _mode;

    /// <summary>Wait time between driving the outputs and reading the inputs.</summary>
    [ObservableProperty]
    private int _settleTimeMs;

    [ObservableProperty]
    private ObservableCollection<BoundaryScanPinAction> _pinActions = new();

    public BoundaryScanStepConfig()
    {
        Subscribe(PinActions);
    }

    public BoundaryScanStepConfig(BoundaryScanStepConfig other) : this()
    {
        Mode = other.Mode;
        SettleTimeMs = other.SettleTimeMs;

        foreach (var action in other.PinActions)
            PinActions.Add(new BoundaryScanPinAction(action));
    }

    partial void OnPinActionsChanged(ObservableCollection<BoundaryScanPinAction>? oldValue, ObservableCollection<BoundaryScanPinAction> newValue)
    {
        if (oldValue != null)
        {
            oldValue.CollectionChanged -= PinActionsCollectionChanged;
            foreach (var action in oldValue)
                action.PropertyChanged -= PinActionChanged;
        }

        Subscribe(newValue);
    }

    private void Subscribe(ObservableCollection<BoundaryScanPinAction> actions)
    {
        actions.CollectionChanged += PinActionsCollectionChanged;
        foreach (var action in actions)
            action.PropertyChanged += PinActionChanged;
    }

    private void PinActionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var action in e.OldItems.OfType<BoundaryScanPinAction>())
                action.PropertyChanged -= PinActionChanged;
        }

        if (e.NewItems != null)
        {
            foreach (var action in e.NewItems.OfType<BoundaryScanPinAction>())
                action.PropertyChanged += PinActionChanged;
        }

        OnPropertyChanged(nameof(PinActions));
    }

    // Lets the owning test step report the change, which marks the project dirty. Computed flags are skipped.
    private void PinActionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BoundaryScanPinAction.DeviceId) or nameof(BoundaryScanPinAction.Port) or nameof(BoundaryScanPinAction.Action))
            OnPropertyChanged(nameof(PinActions));
    }
}