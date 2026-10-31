using ATLab.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ATLab.Controls;

public partial class ExternalProgramEditor : UserControl
{
    public static readonly StyledProperty<ExternalProgramEditorViewModel?> ExternalProgramEditorVmProperty =
        AvaloniaProperty.Register<ExternalProgramEditor, ExternalProgramEditorViewModel?>(nameof(ExternalProgramEditorVm));

    public ExternalProgramEditorViewModel? ExternalProgramEditorVm
    {
        get => GetValue(ExternalProgramEditorVmProperty);
        set => SetValue(ExternalProgramEditorVmProperty, value);
    }
    
    public ExternalProgramEditor()
    {
        InitializeComponent();
    }
}