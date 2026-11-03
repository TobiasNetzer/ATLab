using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.FixtureDesigner.ViewModels;

public enum DialogResult
{
    Cancel,
    Ok,
    Yes,
    No
}

public sealed record DialogButton(string Text, DialogResult Result, bool IsPrimary = false);

/// <summary>Message or question shown over the main window; the view model awaits the button the user clicks.</summary>
public sealed partial class DialogViewModel(string title, string message, IReadOnlyList<DialogButton> buttons, bool isError = false)
{
    private readonly TaskCompletionSource<DialogResult> _result = new();

    public string Title { get; } = title;
    public string Message { get; } = message;
    public IReadOnlyList<DialogButton> Buttons { get; } = buttons;
    public bool IsError { get; } = isError;

    public Task<DialogResult> Result => _result.Task;

    [RelayCommand]
    private void Choose(DialogResult result) => _result.TrySetResult(result);

    /// <summary>Escape or a click beside the dialog: the last button (Cancel or OK).</summary>
    [RelayCommand]
    private void Dismiss() => _result.TrySetResult(Buttons.Count > 0 ? Buttons[^1].Result : DialogResult.Cancel);
}