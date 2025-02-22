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
    private const int WINDOWLAYER = 10000;

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
    public IReadOnlyList<ManagedWindow> Windows => this.Children?.Where(Children => Children is ManagedWindow).Cast<ManagedWindow>().ToArray() ?? Array.Empty<ManagedWindow>();

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

    public void MinimizeAllWindows()
    {
        foreach (var win in Windows)
        {
            win.MinimizeWindow();
        }
    }

    public void ShowAllWindows()
    {
        foreach(var win in Windows)
        {
            win.RestoreWindow();
        }
    }

    public void BringToTop(ManagedWindow window)
    {
        if (window.Owner != null)
        {
            BringToTop(window.Owner);
        }

        foreach (var win in Windows.Where(win => window != win && win.WindowState == WindowState.Minimized))
        {
            win.ZIndex = WINDOWLAYER;
        }

        var windows = Windows.Where(win => win != window && win.WindowState != WindowState.Minimized).OrderBy(win => win.ZIndex);
        int i = WINDOWLAYER + 1;
        foreach (var win in windows)
        {
            win.ZIndex = i++;
        }
        window.ZIndex = i;
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