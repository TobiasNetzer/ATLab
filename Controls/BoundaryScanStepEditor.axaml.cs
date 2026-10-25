using ATLab.ViewModels;
using Avalonia;
using Avalonia.Controls;

namespace ATLab.Controls;

public partial class BoundaryScanStepEditor : UserControl
{
    public static readonly StyledProperty<BoundaryScanStepEditorViewModel?> BoundaryScanStepEditorVmProperty =
        AvaloniaProperty.Register<BoundaryScanStepEditor, BoundaryScanStepEditorViewModel?>(nameof(BoundaryScanStepEditorVm));

    public BoundaryScanStepEditorViewModel? BoundaryScanStepEditorVm
    {
        get => GetValue(BoundaryScanStepEditorVmProperty);
        set => SetValue(BoundaryScanStepEditorVmProperty, value);
    }

    public BoundaryScanStepEditor()
    {
        InitializeComponent();
    }
}