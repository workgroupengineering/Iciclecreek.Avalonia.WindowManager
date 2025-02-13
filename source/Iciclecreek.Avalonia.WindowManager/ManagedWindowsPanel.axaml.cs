using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Iciclecreek.Avalonia.WindowManager;

public partial class ManagedWindowsPanel : Canvas
{
    public ManagedWindowsPanel()
    {
        InitializeComponent();
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
        var windows = GetWindows();
        window.ZIndex = windows.Any() ? windows.Max(child => child.ZIndex) + 1 : 10;
        this.Children.Add(window);
    }

    public IEnumerable<ManagedWindow> GetWindows()
    {
        return this.Children.Where(Children => Children is ManagedWindow).Cast<ManagedWindow>().ToList();
    }
}