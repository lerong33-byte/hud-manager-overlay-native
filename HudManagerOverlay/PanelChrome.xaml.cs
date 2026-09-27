using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace HudManagerOverlay;

// Generic panel wrapper: title bar (drag), close button, resize grip, brand-styled border.
// Any panel's real content (LoadoutBayView, future panels) goes in as Content1 — this is the
// native equivalent of the old detach.html chrome, minus the browser page inside it.
public partial class PanelChrome : UserControl
{
    public event Action? CloseRequested;

    private bool _dragging;
    private Point _dragStart;

    public PanelChrome()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => TitleText.Text;
        set => TitleText.Text = value;
    }

    public object? PanelContent
    {
        get => Content1.Content;
        set => Content1.Content = value;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = true;
        _dragStart = e.GetPosition(Parent as UIElement);
        TitleBar.CaptureMouse();
    }

    private void TitleBar_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging || Parent is not UIElement parent) return;
        var pos = e.GetPosition(parent);
        var dx = pos.X - _dragStart.X;
        var dy = pos.Y - _dragStart.Y;
        _dragStart = pos;
        Canvas.SetLeft(this, Canvas.GetLeft(this) + dx);
        Canvas.SetTop(this, Canvas.GetTop(this) + dy);
    }

    private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        TitleBar.ReleaseMouseCapture();
    }

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        Width = Math.Max(220, ActualWidth + e.HorizontalChange);
        Height = Math.Max(160, ActualHeight + e.VerticalChange);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke();
}
