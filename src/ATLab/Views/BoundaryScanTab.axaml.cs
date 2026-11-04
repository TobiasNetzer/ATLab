using System;
using System.Linq;
using ATLab.ViewModels;
using Avalonia.Controls;

namespace ATLab.Views;

public partial class BoundaryScanTab : UserControl
{
    public BoundaryScanTab()
    {
        InitializeComponent();
    }

    /// <summary>
    /// A connected net is taken as soon as the box holds the exact name of a net of the list: picked from the
    /// suggestions or typed in full. Like the net boxes of the Board and Nets panel, as a click into the suggestions
    /// takes the focus away before the box would hand its text over. Other text is taken when the box loses the focus.
    /// </summary>
    private void OnConnectedNetTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not AutoCompleteBox { DataContext: InterconnectNetRowViewModel row, Text: var text } ||
            DataContext is not BoundaryScanTabViewModel { Interconnect: var interconnect })
            return;

        text ??= string.Empty;
        if (row.ConnectedTo != text && (text.Length == 0 || interconnect.NetNames.Contains(text, StringComparer.Ordinal)))
            row.ConnectedTo = text;
    }
}
