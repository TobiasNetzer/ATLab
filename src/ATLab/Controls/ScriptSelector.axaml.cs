using ATLab.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ATLab.Controls;

public partial class ScriptSelector : UserControl
{
    public static readonly StyledProperty<ScriptSelectorViewModel?> ScriptSelectorVmProperty =
        AvaloniaProperty.Register<ScriptSelector, ScriptSelectorViewModel?>(nameof(ScriptSelectorVm));

    public ScriptSelectorViewModel? ScriptSelectorVm
    {
        get => GetValue(ScriptSelectorVmProperty);
        set => SetValue(ScriptSelectorVmProperty, value);
    }

    
    public ScriptSelector()
    {
        InitializeComponent();
    }
}
