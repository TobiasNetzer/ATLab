using System.ComponentModel;
using System.Linq;
using ATLab.Analyzer.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace ATLab.Analyzer.Views;

public partial class MainWindow : ShadUI.Window
{
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);

        // A tab builds its grid when it is shown first and does not take over a selection made before, e.g. a step
        // opened from the failure list of the overview; select and reveal it once the grid is there
        StepsGrid.AttachedToVisualTree += (_, _) => SyncSelection(StepsGrid, _viewModel?.SelectedStep);
        RunsGrid.AttachedToVisualTree += (_, _) => SyncSelection(RunsGrid, _viewModel?.SelectedRun);
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

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedStep))
            SyncSelection(StepsGrid, _viewModel?.SelectedStep);
        else if (e.PropertyName == nameof(MainWindowViewModel.SelectedRun))
            SyncSelection(RunsGrid, _viewModel?.SelectedRun);
    }

    /// <summary>Selects the item in the grid and scrolls it into view, after the grid has laid out its rows.</summary>
    private static void SyncSelection(DataGrid grid, object? item)
    {
        if (item is null)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            if (!grid.IsVisible)
                return;

            if (!Equals(grid.SelectedItem, item))
                grid.SelectedItem = item;

            grid.ScrollIntoView(item, null);
        }, DispatcherPriority.Loaded);
    }

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    /// <summary>Dropped files and folders are imported like opened ones.</summary>
    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var paths = e.DataTransfer.TryGetFiles()?
            .Select(item => item.TryGetLocalPath())
            .OfType<string>()
            .ToList();

        if (paths is { Count: > 0 })
            await vm.LoadPathsAsync(paths);
    }
}