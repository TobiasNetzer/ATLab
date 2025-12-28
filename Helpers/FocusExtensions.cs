using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ATLab.Helpers;

public static class FocusExtensions
{
    public static readonly AttachedProperty<bool> MoveFocusOnEnterProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("MoveFocusOnEnter", typeof(FocusExtensions));

    public static bool GetMoveFocusOnEnter(Control element) => element.GetValue(MoveFocusOnEnterProperty);
    public static void SetMoveFocusOnEnter(Control element, bool value) => element.SetValue(MoveFocusOnEnterProperty, value);

    static FocusExtensions()
    {
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
}
