using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using System;
using System.Diagnostics;

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
}