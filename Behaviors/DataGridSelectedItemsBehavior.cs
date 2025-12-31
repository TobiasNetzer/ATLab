using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls;

namespace ATLab.Behaviors;

public static class DataGridSelectedItemsBehavior
{
    public static readonly AttachedProperty<IList?> SelectedItemsProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, IList?>(
            "SelectedItems", typeof(DataGridSelectedItemsBehavior));

    public static void SetSelectedItems(AvaloniaObject element, IList? value) =>
        element.SetValue(SelectedItemsProperty, value);

    public static IList? GetSelectedItems(AvaloniaObject element) =>
        element.GetValue(SelectedItemsProperty);

    static DataGridSelectedItemsBehavior()
    {
        SelectedItemsProperty.Changed.Subscribe(new Observer());
    }

    private class Observer : IObserver<AvaloniaPropertyChangedEventArgs<IList?>>
    {
        public void OnNext(AvaloniaPropertyChangedEventArgs<IList?> args)
        {
            if (args.Sender is DataGrid grid)
            {
                grid.SelectionChanged += (_, __) =>
                {
                    var target = GetSelectedItems(grid);
                    if (target != null)
                    {
                        target.Clear();
                        foreach (var item in grid.SelectedItems)
                            target.Add(item);
                    }
                };
            }
        }

        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }
}