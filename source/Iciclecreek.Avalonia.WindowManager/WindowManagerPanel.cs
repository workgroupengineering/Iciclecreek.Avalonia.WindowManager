using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        this.Children.Add(window);

        // Force a layout pass
        window.Measure(Size.Infinity);
        window.Arrange(new Rect(window.DesiredSize));

        SetWindowStartupLocation(window);

        window.Show();

        if (window.ShowActivated)
        {
            window.Activate();
        }
    }

    //private WindowStartupLocation GetEffectiveWindowStartupLocation(ManagedWindow? owner)
    //{
    //    var startupLocation = thisWindowStartupLocation;

    //    if (startupLocation == WindowStartupLocation.CenterOwner &&
    //        (owner is null ||
    //         (owner is ManagedWindow ownerWindow && ownerWindow.WindowState == WindowState.Minimized))
    //       )
    //    {
    //        // If startup location is CenterOwner, but owner is null or minimized then fall back
    //        // to CenterScreen. This behavior is consistent with WPF.
    //        startupLocation = WindowStartupLocation.CenterScreen;
    //    }

    //    return startupLocation;
    //}



    public void ShowWindow(ManagedWindow window, double x, double y)
    {
        Canvas.SetLeft(window, x);
        Canvas.SetTop(window, y);

        this.Children.Add(window);

        window.Show();
    }

    public void BringToTop(ManagedWindow window)
    {
        var windows = Windows.Where(win => win != window).OrderBy(win => win.ZIndex);
        int i = 10000;
        foreach (var win in windows)
        {
            win.ZIndex = i++;
            Debug.WriteLine($"{win} ZIndex: {win.ZIndex}");
        }
        window.ZIndex = i;
        Debug.WriteLine($"{window} ZIndex: {window.ZIndex}");

    }

    private void SetWindowStartupLocation(ManagedWindow window)
    {
        var startupLocation = GetEffectiveWindowStartupLocation(window);

        PixelRect size;
        switch (window.SizeToContent)
        {
            case SizeToContent.Manual:
                size = new PixelRect(0, 0, (int)window.Width, (int)window.Height);
                break;
            case SizeToContent.WidthAndHeight:
                size = new PixelRect(0, 0, (int)window.DesiredSize.Width, (int)window.DesiredSize.Height);
                break;

            case SizeToContent.Width:
                size = new PixelRect(0, 0, (int)window.DesiredSize.Width, (int)window.Height);
                break;
            case SizeToContent.Height:
                size = new PixelRect(0, 0, (int)window.Width, (int)window.DesiredSize.Height);
                break;
            default:
                throw new NotImplementedException();
        }

        var screenSize = new PixelRect(0, 0, (int)this.Bounds.Width, (int)this.Bounds.Height);

        if (startupLocation == WindowStartupLocation.CenterOwner)
        {
            if (window.Owner != null)
            {
                var ownerRect = new PixelRect(
                    window.Owner.Position,
                    new PixelSize((int)window.Owner.Bounds.Width, (int)window.Owner.Bounds.Height));
                var childRect = ownerRect.CenterRect(size);
                window.Position = childRect.Position;
                return;
            }
            else
            {
                var childRect = screenSize.CenterRect(size);
                window.Position = childRect.Position;
            }
        }
        else if (startupLocation == WindowStartupLocation.CenterScreen)
        {
            var childRect = screenSize.CenterRect(size);
            window.Position = childRect.Position;
        }

    }

    private WindowStartupLocation GetEffectiveWindowStartupLocation(ManagedWindow window)
    {
        if (window.WindowStartupLocation == WindowStartupLocation.CenterOwner &&
            (window.Owner is null ||
             (window.Owner != null && window.Owner.WindowState == WindowState.Minimized)))
        {
            // If startup location is CenterOwner, but owner is null or minimized then fall back
            // to CenterScreen. This behavior is consistent with WPF.
            return WindowStartupLocation.CenterScreen;
        }

        return window.WindowStartupLocation;
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


}