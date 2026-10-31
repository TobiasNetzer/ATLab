using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace ATLab.Helpers;

/// <summary>
/// Attached properties for keyboard focus: move to the next field on Enter, and focus a control when it becomes
/// visible.
/// </summary>
public static class FocusExtensions
{
    public static readonly AttachedProperty<bool> MoveFocusOnEnterProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("MoveFocusOnEnter", typeof(FocusExtensions));

    public static bool GetMoveFocusOnEnter(Control element) => element.GetValue(MoveFocusOnEnterProperty);
    public static void SetMoveFocusOnEnter(Control element, bool value) => element.SetValue(MoveFocusOnEnterProperty, value);

    public static readonly AttachedProperty<bool> FocusWhenVisibleProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("FocusWhenVisible", typeof(FocusExtensions));

    public static bool GetFocusWhenVisible(Control element) => element.GetValue(FocusWhenVisibleProperty);
    public static void SetFocusWhenVisible(Control element, bool value) => element.SetValue(FocusWhenVisibleProperty, value);

    static FocusExtensions()
    {
        // focus when the control is shown, either by being loaded (e.g. dialog opened) or by becoming visible
        Control.LoadedEvent.AddClassHandler<Control>((control, _) => FocusIfVisible(control));
        Visual.IsVisibleProperty.Changed.AddClassHandler<Control>((control, _) => FocusIfVisible(control));


        InputElement.KeyDownEvent.AddClassHandler<Control>((control, e) =>
        {
            if (GetMoveFocusOnEnter(control) && (e.Key == Key.Enter || e.Key == Key.Return))
            {
                var focusManager = TopLevel.GetTopLevel(control)?.FocusManager;
                if (focusManager != null)
                {
                    e.Handled = true;
                    
                    var moveFocusMethod = focusManager.GetType().GetMethod("MoveFocus", new[] { typeof(NavigationDirection), typeof(KeyModifiers) });
                    if (moveFocusMethod != null)
                    {
                        moveFocusMethod.Invoke(focusManager, new object?[] { NavigationDirection.Next, e.KeyModifiers });
                    }
                    else
                    {
                        var tabArgs = new KeyEventArgs
                        {
                            RoutedEvent = InputElement.KeyDownEvent,
                            Key = Key.Tab,
                            KeyModifiers = e.KeyModifiers,
                            Source = control
                        };
                        control.RaiseEvent(tabArgs);
                    }
                }
            }
        }, RoutingStrategies.Bubble);
    }

    private static void FocusIfVisible(Control control)
    {
        if (!GetFocusWhenVisible(control) || !control.IsVisible)
            return;

        // post so the focus is set after the dialog host has finished setting up its own focus
        Dispatcher.UIThread.Post(() =>
        {
            if (control.IsEffectivelyVisible)
                control.Focus();
        }, DispatcherPriority.Background);
    }
}
