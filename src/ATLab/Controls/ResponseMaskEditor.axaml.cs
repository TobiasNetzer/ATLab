using ATLab.ViewModels;
using Avalonia;
using Avalonia.Controls;

namespace ATLab.Controls;

public partial class ResponseMaskEditor : UserControl
{
    public static readonly StyledProperty<ResponseMaskEditorViewModel?> ResponseMaskEditorVmProperty =
        AvaloniaProperty.Register<ResponseMaskEditor, ResponseMaskEditorViewModel?>(nameof(ResponseMaskEditorVm));

    public ResponseMaskEditorViewModel? ResponseMaskEditorVm
    {
        get => GetValue(ResponseMaskEditorVmProperty);
        set => SetValue(ResponseMaskEditorVmProperty, value);
    }
    
    public ResponseMaskEditor()
    {
        InitializeComponent();
    }
}