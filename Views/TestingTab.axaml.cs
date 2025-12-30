using Avalonia.Controls;

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
}