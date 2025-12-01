using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ATLab.Controls;

public partial class StimSelector : UserControl
{
    public static readonly StyledProperty<int> GridColumnsProperty =
        AvaloniaProperty.Register<StimSelector, int>(nameof(GridColumns), 2);

    public static readonly StyledProperty<int> GridRowsProperty =
        AvaloniaProperty.Register<StimSelector, int>(nameof(GridRows), 8);

    public int GridColumns
    {
        get => GetValue(GridColumnsProperty);
        set => SetValue(GridColumnsProperty, value);
    }

    public int GridRows
    {
        get => GetValue(GridRowsProperty);
        set => SetValue(GridRowsProperty, value);
    }
    
    public StimSelector()
    {
        InitializeComponent();
    }
}