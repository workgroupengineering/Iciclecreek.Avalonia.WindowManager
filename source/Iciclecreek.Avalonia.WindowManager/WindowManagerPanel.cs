using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Iciclecreek.Avalonia.WindowManager;

public class WindowManagerPanel : Canvas
{
    public WindowManagerPanel()
    {
        ManagedWindow.WindowClosedEvent.AddClassHandler(typeof(ManagedWindow), (sender, _) =>
        {
            var window = (ManagedWindow)sender!;
            Children.Remove(window);
            
            if (window.Owner != null)
            {
                window.Owner.Activate();
            }
            else
            {
                this.Windows.LastOrDefault()?.Activate();
            }
        });
    }

    /// <summary>
    /// Gets a collection of child windows owned by this window.
    /// </summary>
    public IReadOnlyList<ManagedWindow> Windows => this.Children.Where(Children => Children is ManagedWindow).Cast<ManagedWindow>().ToArray();

    public void ShowWindow(ManagedWindow window)
    {
        switch (window.WindowStartupLocation)
        {
            case WindowStartupLocation.CenterOwner:
            case WindowStartupLocation.CenterScreen:
                Canvas.SetLeft(window, (Bounds.Width - window.Width) / 2);
                Canvas.SetTop(window, (Bounds.Height - window.Height) / 2);
                break;
            case WindowStartupLocation.Manual:
                Canvas.SetLeft(window, window.Position.X);
                Canvas.SetTop(window, window.Position.Y);
                break;
        }

        window.ZIndex = GetTopZIndex() + 1;
        this.Children.Add(window);
    }


    public void ShowWindow(ManagedWindow window, double x, double y)
    {
        Canvas.SetLeft(window, x);
        Canvas.SetTop(window, y);
        BringToTop(window);

        this.Children.Add(window);

        window.Show();
        window.Activate();
    }

    public void BringToTop(ManagedWindow window)
    {
        window.ZIndex = GetTopZIndex() + 1;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        this.Windows.LastOrDefault()?.Activate();
    }


    /// <summary>
    /// Measures the control and its child elements as part of a layout pass.
    /// </summary>
    /// <param name="availableSize">The size available to the control.</param>
    /// <returns>The desired size for the control.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0;
        double height = 0;

        var visualChildren = VisualChildren;
        var visualCount = visualChildren.Count;

        for (var i = 0; i < visualCount; i++)
        {
            Visual visual = visualChildren[i];

            if (visual is Layoutable layoutable)
            {
                layoutable.Measure(availableSize);
                width = Math.Max(width, layoutable.DesiredSize.Width);
                height = Math.Max(height, layoutable.DesiredSize.Height);
            }
        }

        return new Size(width, height);
    }


    private int GetTopZIndex()
        => Windows.Count > 0 ? Windows.Max(child => child.ZIndex) : 100;
}