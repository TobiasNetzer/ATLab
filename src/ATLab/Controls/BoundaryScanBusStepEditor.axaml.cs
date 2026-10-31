using ATLab.ViewModels;
using Avalonia;
using Avalonia.Controls;

namespace ATLab.Controls;

public partial class BoundaryScanBusStepEditor : UserControl
{
    public static readonly StyledProperty<BoundaryScanBusStepEditorViewModel?> BoundaryScanBusStepEditorVmProperty =
        AvaloniaProperty.Register<BoundaryScanBusStepEditor, BoundaryScanBusStepEditorViewModel?>(nameof(BoundaryScanBusStepEditorVm));

    public BoundaryScanBusStepEditorViewModel? BoundaryScanBusStepEditorVm
    {
        get => GetValue(BoundaryScanBusStepEditorVmProperty);
        set => SetValue(BoundaryScanBusStepEditorVmProperty, value);
    }

    public BoundaryScanBusStepEditor()
    {
        InitializeComponent();
    }
}