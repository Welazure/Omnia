using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Omnia.Client.ViewModels;

namespace Omnia.Client.Views;

public partial class ClipboardView : UserControl
{
    private const double SwipeThreshold = 60;
    private const double DragThreshold = 8;

    private double pressX;
    private bool dragging;

    public ClipboardView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => ClipInput.Focus();
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        (DataContext as ClipboardViewModel)?.SubmitCommand.Execute(null);
    }

    private void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        pressX = e.GetPosition(this).X;
        dragging = false;
        e.Pointer.Capture(sender as IInputElement);
    }

    private void OnItemPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Border border || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var delta = e.GetPosition(this).X - pressX;
        if (!dragging && Math.Abs(delta) < DragThreshold)
        {
            return;
        }

        dragging = true;
        border.RenderTransform = new TranslateTransform(delta, 0);
    }

    private void OnItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not Border border)
        {
            return;
        }

        var delta = e.GetPosition(this).X - pressX;
        border.RenderTransform = null;
        e.Pointer.Capture(null);

        var viewModel = DataContext as ClipboardViewModel;
        if (border.DataContext is ClipListItem item)
        {
            if (dragging && Math.Abs(delta) >= SwipeThreshold)
            {
                viewModel?.DeleteClipCommand.Execute(item);
            }
            else if (!dragging)
            {
                viewModel?.CopyClipCommand.Execute(item);
            }
        }

        dragging = false;
    }
}
