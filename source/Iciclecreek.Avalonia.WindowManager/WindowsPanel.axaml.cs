using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Iciclecreek.Avalonia.WindowManager;

[TemplatePart(PART_Windows, typeof(Canvas))]
public class WindowsPanel : ContentControl
{
    private const int WINDOWLAYER = 10000;

    private const string PART_Windows = "PART_Windows";

    private Canvas _canvas;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public WindowsPanel()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        ManagedWindow.WindowClosedEvent.AddClassHandler(typeof(ManagedWindow), (sender, _) =>
        {
            var window = (ManagedWindow)sender!;
            _canvas.Children.Remove(window);
            
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

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _canvas = (Canvas)e.NameScope.Find<Control>(PART_Windows) ?? throw new ArgumentNullException(PART_Windows);
    }

    /// <summary>de
    /// Gets a collection of child windows owned by this window.
    /// </summary>
    public IReadOnlyList<ManagedWindow> Windows => this._canvas?.Children.Cast<ManagedWindow>().ToArray() ?? Array.Empty<ManagedWindow>();

    public void AddWindow(ManagedWindow window)
    {
        this._canvas.Children.Add(window);
        window.Closed += Window_Closed;

        // Force a layout pass
        window.Measure(Size.Infinity);
        window.Arrange(new Rect(window.DesiredSize));

        SetWindowStartupLocation(window);
        window.Show();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        var window = sender as ManagedWindow;
        window.Closed -= Window_Closed;
        this._canvas.Children.Remove(window);
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

        var screenSize = new PixelRect(0, 0, (int)this.Bounds.Width, (int)this.Bounds.Height);

        PixelRect size;
        switch (window.SizeToContent)
        {
            case SizeToContent.Manual:
                size = new PixelRect(0, 0, 
                                     (int)(Double.IsNaN(window.Width) ? window.DesiredSize.Width : window.Width), 
                                     (int)(Double.IsNaN(window.Height) ? window.DesiredSize.Height : window.Height));
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