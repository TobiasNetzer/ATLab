using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Threading;

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
        SelectedItemsProperty.Changed.Subscribe(new SelectedItemsObserver());
    }

    private class SelectedItemsObserver : IObserver<AvaloniaPropertyChangedEventArgs<IList?>>
    {
        public void OnNext(AvaloniaPropertyChangedEventArgs<IList?> args)
        {
            if (args.Sender is not DataGrid grid)
                return;

            grid.AttachedToVisualTree -= GridOnAttached;
            grid.AttachedToVisualTree += GridOnAttached;
        }

        private void GridOnAttached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (sender is not DataGrid grid)
                return;

            grid.SelectionChanged -= GridOnSelectionChanged;
            grid.SelectionChanged += GridOnSelectionChanged;
        }

        private static void GridOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is not DataGrid grid)
                return;

            var target = GetSelectedItems(grid);
            if (target == null)
                return;

            target.Clear();
            foreach (var item in grid.SelectedItems)
                target.Add(item);

            if (grid.SelectedItem == null)
                return;
            
            Dispatcher.UIThread.Post(() =>
            {
                var row = FindRowByDataContext(grid, grid.SelectedItem);
                row?.BringIntoView();
            });
        }

        private static Control? FindRowByDataContext(DataGrid grid, object item)
        {
            foreach (var v in grid.GetVisualDescendants())
            {
                if (v is Control c && c.DataContext == item)
                    return c;
            }

            return null;
        }

        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }
}