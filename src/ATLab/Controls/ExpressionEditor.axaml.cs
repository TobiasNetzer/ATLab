using ATLab.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ATLab.Controls;

public partial class ExpressionEditor : UserControl
{
    public static readonly StyledProperty<ExpressionEditorViewModel?> ExpressionEditorVmProperty =
        AvaloniaProperty.Register<ExpressionEditor, ExpressionEditorViewModel?>(nameof(ExpressionEditorVm));

    public ExpressionEditorViewModel? ExpressionEditorVm
    {
        get => GetValue(ExpressionEditorVmProperty);
        set => SetValue(ExpressionEditorVmProperty, value);
    }
    
    public ExpressionEditor()
    {
        InitializeComponent();
    }
}