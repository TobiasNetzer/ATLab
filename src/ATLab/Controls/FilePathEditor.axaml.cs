using ATLab.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ATLab.Controls;

public partial class FilePathEditor : UserControl
{
    public static readonly StyledProperty<FilePathEditorViewModel?> FilePathEditorVmProperty =
        AvaloniaProperty.Register<FilePathEditor, FilePathEditorViewModel?>(nameof(FilePathEditorVm));

    public FilePathEditorViewModel? FilePathEditorVm
    {
        get => GetValue(FilePathEditorVmProperty);
        set => SetValue(FilePathEditorVmProperty, value);
    }
    public FilePathEditor()
    {
        InitializeComponent();
    }
}