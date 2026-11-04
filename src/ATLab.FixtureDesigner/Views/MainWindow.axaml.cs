using System.ComponentModel;
using System.Linq;
using ATLab.FixtureDesigner.Fixture;
using ATLab.Pcb.Fixture;
using ATLab.FixtureDesigner.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace ATLab.FixtureDesigner.Views;

public partial class MainWindow : ShadUI.Window
{
    private MainWindowViewModel? _viewModel;

    /// <summary>Set once the user agreed to close with or without saving.</summary>
    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();

        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);

        // A list on a hidden tab cannot take a new selection and keeps its old one; it catches up when shown
        ProbesGrid.AttachedToVisualTree += (_, _) => Resync(ProbesGrid, () => _viewModel?.SelectedProbeItem);
        PadsGrid.AttachedToVisualTree += (_, _) => Resync(PadsGrid, () => _viewModel?.SelectedPadRow);
    }

    private static void Resync(DataGrid grid, System.Func<object?> selected)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var item = selected();
            if (!grid.IsVisible || Equals(grid.SelectedItem, item))
                return;

            grid.SelectedItem = item;
            if (item is not null)
                grid.ScrollIntoView(item, null);
        }, DispatcherPriority.Loaded);
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = DataContext as MainWindowViewModel;

        if (_viewModel is not null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>A probe or pad selected on the board is revealed in its list.</summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedProbeItem))
            Reveal(ProbesGrid, _viewModel?.SelectedProbeItem);
        else if (e.PropertyName == nameof(MainWindowViewModel.SelectedPadRow))
            Reveal(PadsGrid, _viewModel?.SelectedPadRow);
    }

    private static void Reveal(DataGrid grid, object? item)
    {
        if (item is null)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            if (grid.IsVisible)
                grid.ScrollIntoView(item, null);
        }, DispatcherPriority.Loaded);
    }

    /// <summary>Unsaved changes: ask before the window closes.</summary>
    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (_closeConfirmed || _viewModel is not { IsDirty: true })
            return;

        e.Cancel = true;
        if (await _viewModel.ConfirmDiscardAsync())
        {
            _closeConfirmed = true;
            Close();
        }
    }

    /// <summary>
    /// Pin chosen in the probe list. Only choices of the user are applied: the grid reuses its rows, and the one-way
    /// binding also changes the selection when a row shows another probe.
    /// </summary>
    private void OnPinSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { DataContext: Probe probe } combo)
            return;
        if (!combo.IsDropDownOpen && !combo.IsKeyboardFocusWithin)
            return;

        if (combo.SelectedItem is PinOption option && option.Pin != probe.Pin)
            probe.Pin = option.Pin;
    }

    /// <summary>Start of a wire chosen; like the pin, only choices of the user are applied.</summary>
    private void OnWireFromChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { DataContext: Wire wire } combo && IsUserChoice(combo) &&
            combo.SelectedItem is WireEndpoint end && end != wire.From)
            wire.From = end;
    }

    /// <summary>End of a wire chosen; like the pin, only choices of the user are applied.</summary>
    private void OnWireToChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { DataContext: Wire wire } combo && IsUserChoice(combo) &&
            combo.SelectedItem is WireEndpoint end && end != wire.To)
            wire.To = end;
    }

    private static bool IsUserChoice(ComboBox combo) => combo.IsDropDownOpen || combo.IsKeyboardFocusWithin;

    /// <summary>Probe type chosen in the probe list; like the pin, only choices of the user are applied.</summary>
    private void OnTypeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { DataContext: Probe probe } combo)
            return;
        if (!combo.IsDropDownOpen && !combo.IsKeyboardFocusWithin)
            return;

        if (combo.SelectedItem is ProbeType type && type != probe.Type)
            probe.Type = type;
    }

    /// <summary>Type chosen for a part; like the probes' type, only choices of the user are applied.</summary>
    private void OnPartTypeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { DataContext: FixturePart part } combo)
            return;
        if (!combo.IsDropDownOpen && !combo.IsKeyboardFocusWithin)
            return;

        if (combo.SelectedItem is PartType type && type != part.Type)
            part.Type = type;
    }

    /// <summary>Kind chosen for a part type; only choices of the user are applied.</summary>
    private void OnPartKindSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { DataContext: PartType type } combo)
            return;
        if (!combo.IsDropDownOpen && !combo.IsKeyboardFocusWithin)
            return;

        if (combo.SelectedItem is PartKindOption option && option.Kind != type.Kind)
            type.Kind = option.Kind;
    }

    private void OnFitClicked(object? sender, RoutedEventArgs e) => Board.FitToView();

    private void OnChecksClicked(object? sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
            ChecksTab.IsSelected = true;
    }

    /// <summary>Double-click in the pad list: like a double-click on the board.</summary>
    private void OnPadRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel?.SelectedPadRow is { } row && e.Source is Control { DataContext: PadRow })
            _viewModel.TogglePadCommand.Execute(row.Pad);
    }

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    /// <summary>A dropped fixture or board is opened.</summary>
    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var path = e.DataTransfer.TryGetFiles()?
            .Select(item => item.TryGetLocalPath())
            .OfType<string>()
            .FirstOrDefault();

        if (path is not null)
            await vm.OpenPathAsync(path);
    }
}