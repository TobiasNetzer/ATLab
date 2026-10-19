using Avalonia.Controls;
using Avalonia.Input;

namespace ATLab.Views;

public partial class TestingTab : UserControl
{
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
    }

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
