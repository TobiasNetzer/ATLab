using ATLab.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ATLab.Controls;

public partial class ShellCommandEditor : UserControl
{
    public static readonly StyledProperty<ShellCommandEditorViewModel?> ShellCommandEditorVmProperty =
        AvaloniaProperty.Register<ShellCommandEditor, ShellCommandEditorViewModel?>(nameof(ShellCommandEditorVm));

    public ShellCommandEditorViewModel? ShellCommandEditorVm
    {
        get => GetValue(ShellCommandEditorVmProperty);
        set => SetValue(ShellCommandEditorVmProperty, value);
    }
    
    public ShellCommandEditor()
    {
        InitializeComponent();
    }
}