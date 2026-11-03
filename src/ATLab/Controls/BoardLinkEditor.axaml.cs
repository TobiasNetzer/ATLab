using System;
using System.Linq;
using ATLab.ViewModels;
using Avalonia.Controls;

namespace ATLab.Controls;

/// <summary>Board of the device under test and the nets of the adapter pins, in the configuration tab.</summary>
public partial class BoardLinkEditor : UserControl
{
    public BoardLinkEditor() => InitializeComponent();

    /// <summary>
    /// A net of the board is taken as soon as the box holds its exact name: picked from the suggestions (click, Enter,
    /// arrow keys) or typed in full. Otherwise the box would only hand its text over when it loses the focus, and a
    /// click into the list already took the focus away while the typed text (e.g. "gn") was still in it. Other text,
    /// such as a net not on the board, is still taken when the box loses the focus.
    /// </summary>
    private void OnNetTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not AutoCompleteBox { DataContext: PinNetRow row, Text: { Length: > 0 } text } box ||
            DataContext is not BoardLinkEditorViewModel editor)
            return;

        if (row.Net != text && editor.NetNames.Contains(text, StringComparer.Ordinal))
            row.Net = text;
    }
}