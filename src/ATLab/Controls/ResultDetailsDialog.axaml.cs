using ATLab.ViewModels;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace ATLab.Controls;

public partial class ResultDetailsDialog : UserControl
{
    public ResultDetailsDialog()
    {
        InitializeComponent();
    }

    private async void OnCopyClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ResultDetailsDialogViewModel vm)
            return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
            await clipboard.SetTextAsync(vm.Details);
    }
}
