using System.ComponentModel;
using ATLab.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ATLab.Views;

/// <summary>Testing tab view.</summary>
public partial class TestingTab : UserControl
{
    private TestingTabViewModel? _viewModel;

    public TestingTab()
    {
        InitializeComponent();

        DataGrid? grid = this.FindControl<DataGrid>("TestStepPresenter");
        if (grid == null) return;
        grid.SelectionChanged += (_, e) =>
        {
            if (grid.SelectedItem != null)
                grid.ScrollIntoView(grid.SelectedItem, null);
        };

        // A double-click on a step row shows its result details; headers and empty space are ignored
        grid.DoubleTapped += (_, e) =>
        {
            var row = (e.Source as Visual)?.FindAncestorOfType<DataGridRow>(includeSelf: true);
            if (row?.DataContext is TestStepViewModel { HasResultDetails: true } step)
                _viewModel?.ShowResultDetailsOf(step);
        };

        // The user's split between step list and board view is kept
        BoardSplitter.DragCompleted += (_, _) =>
        {
            var list = StepArea.RowDefinitions[0].ActualHeight;
            var board = StepArea.RowDefinitions[2].ActualHeight;
            if (_viewModel is not null && list + board > 0)
                _viewModel.BoardViewer.HeightRatio = board / (list + board);
        };
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_viewModel is not null)
            _viewModel.BoardViewer.PropertyChanged -= OnBoardViewerChanged;

        _viewModel = DataContext as TestingTabViewModel;

        if (_viewModel is not null)
            _viewModel.BoardViewer.PropertyChanged += OnBoardViewerChanged;

        UpdateBoardHeight();
    }

    private void OnBoardViewerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BoardViewerViewModel.IsVisible))
            UpdateBoardHeight();
    }

    // Smallest heights the splitter leaves to the step list and the board view
    private const double MinStepListHeight = 120;
    private const double MinBoardViewHeight = 260;

    /// <summary>Hidden: the step list takes the whole height. Shown: the split the user chose last.</summary>
    private void UpdateBoardHeight()
    {
        var rows = StepArea.RowDefinitions;
        rows[0].MinHeight = MinStepListHeight;
        if (_viewModel?.BoardViewer is { IsVisible: true } viewer)
        {
            rows[0].Height = new GridLength(1 - viewer.HeightRatio, GridUnitType.Star);
            rows[2].Height = new GridLength(viewer.HeightRatio, GridUnitType.Star);
            rows[2].MinHeight = MinBoardViewHeight;
        }
        else
        {
            rows[0].Height = new GridLength(1, GridUnitType.Star);
            rows[2].Height = GridLength.Auto;
            rows[2].MinHeight = 0;
        }
    }

    private void OnBoardFitClicked(object? sender, RoutedEventArgs e) => BoardView.FitToView();

    // F2 jumps into the step name field
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F2 && e.KeyModifiers == KeyModifiers.None)
        {
            StepConfigurator.FocusStepName();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
}
