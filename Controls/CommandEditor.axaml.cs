using ATLab.ViewModels;
using Avalonia;
using Avalonia.Controls;

namespace ATLab.Controls;

public partial class CommandEditor : UserControl
{
    public static readonly StyledProperty<CommandEditorViewModel?> CommandEditorVmProperty =
        AvaloniaProperty.Register<CommandEditor, CommandEditorViewModel?>(nameof(CommandEditorVm));

    public CommandEditorViewModel? CommandEditorVm
    {
        get => GetValue(CommandEditorVmProperty);
        set => SetValue(CommandEditorVmProperty, value);
    }
    
    public CommandEditor()
    {
        InitializeComponent();
    }
}