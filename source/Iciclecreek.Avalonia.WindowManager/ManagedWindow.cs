using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Iciclecreek.Avalonia.WindowManager;

[TemplatePart(PART_TitleBar, typeof(Control))]
[TemplatePart(PART_MinimizeButton, typeof(Button))]
[TemplatePart(PART_MaximizeButton, typeof(Button))]
[TemplatePart(PART_RestoreButton, typeof(Button))]
[TemplatePart(PART_CloseButton, typeof(Button))]
[TemplatePart(PART_ResizeBorder, typeof(Border))]
[PseudoClasses(":minimized", ":maximized", ":normal", ":dragging")]
public class ManagedWindow : ContentControl
{
    public const string PART_TitleBar = "PART_TitleBar";
    public const string PART_MinimizeButton = "PART_MinimizeButton";
    public const string PART_MaximizeButton = "PART_MaximizeButton";
    public const string PART_RestoreButton = "PART_RestoreButton";
    public const string PART_CloseButton = "PART_CloseButton";
    public const string PART_ResizeBorder = "PART_ResizeBorder";

    /// <summary>
    /// Defines the <see cref="SizeToContent"/> property.
    /// </summary>
    public static readonly StyledProperty<SizeToContent> SizeToContentProperty =
        AvaloniaProperty.Register<ManagedWindow, SizeToContent>(nameof(SizeToContent));

    /// <summary>
    /// Defines the <see cref="ShowActivated"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowActivatedProperty =
        AvaloniaProperty.Register<ManagedWindow, bool>(nameof(ShowActivated), true);

    public static readonly StyledProperty<bool> IsCloseButtonVisibleProperty =
        AvaloniaProperty.Register<ManagedWindow, bool>(nameof(IsCloseButtonVisible), true);

    /// <summary>
    /// Represents the current window state (normal, minimized, maximized)
    /// </summary>
    public static readonly StyledProperty<WindowState> WindowStateProperty =
        AvaloniaProperty.Register<ManagedWindow, WindowState>(nameof(WindowState));

    /// <summary>
    /// Defines the <see cref="Title"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ManagedWindow, string?>(nameof(Title), "Window");

    /// <summary>
    /// Defines the <see cref="Icon"/> property.
    /// </summary>
    public static readonly StyledProperty<WindowIcon?> IconProperty =
        AvaloniaProperty.Register<ManagedWindow, WindowIcon?>(nameof(Icon));

    /// <summary>
    /// Defines the <see cref="WindowStartupLocation"/> property.
    /// </summary>
    public static readonly StyledProperty<WindowStartupLocation> WindowStartupLocationProperty =
        AvaloniaProperty.Register<ManagedWindow, WindowStartupLocation>(nameof(WindowStartupLocation));

    public static readonly StyledProperty<PixelPoint> PositionProperty =
    AvaloniaProperty.Register<ManagedWindow, PixelPoint>(nameof(Position));

    public static readonly StyledProperty<bool> CanResizeProperty =
        AvaloniaProperty.Register<ManagedWindow, bool>(nameof(CanResize), true);

    /// <summary>
    /// Routed event that can be used for global tracking of window destruction
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> WindowClosedEvent =
        RoutedEvent.Register<ManagedWindow, RoutedEventArgs>("WindowClosed", RoutingStrategies.Direct);

    /// <summary>
    /// Routed event that can be used for global tracking of opening windows
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> WindowOpenedEvent =
        RoutedEvent.Register<ManagedWindow, RoutedEventArgs>("WindowOpened", RoutingStrategies.Direct);

    private double _normalWidth;
    private double _normalHeight;

    public ManagedWindow()
    {
    }


    /// <summary>
    /// Gets or sets a value indicating how the window will size itself to fit its content.
    /// </summary>
    /// <remarks>
    /// If <see cref="SizeToContent"/> has a value other than <see cref="SizeToContent.Manual"/>,
    /// <see cref="SizeToContent"/> is automatically set to <see cref="SizeToContent.Manual"/>
    /// if a user resizes the window by using the resize grip or dragging the border.
    /// 
    /// NOTE: Because of a limitation of X11, <see cref="SizeToContent"/> will be reset on X11 to
    /// <see cref="SizeToContent.Manual"/> on any resize - including the resize that happens when
    /// the window is first shown. This is because X11 resize notifications are asynchronous and
    /// there is no way to know whether a resize came from the user or the layout system. To avoid
    /// this, consider setting <see cref="CanResize"/> to false, which will disable user resizing
    /// of the window.
    /// </remarks>
    public SizeToContent SizeToContent
    {
        get => GetValue(SizeToContentProperty);
        set => SetValue(SizeToContentProperty, value);
    }

    /// <summary>
    /// Gets or sets the title of the window.
    /// </summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool IsCloseButtonVisible
    {
        get => GetValue(IsCloseButtonVisibleProperty);
        set => SetValue(IsCloseButtonVisibleProperty, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates whether a window is activated when first shown. 
    /// </summary>
    public bool ShowActivated
    {
        get => GetValue(ShowActivatedProperty);
        set => SetValue(ShowActivatedProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimized/maximized state of the window.
    /// </summary>
    public WindowState WindowState
    {
        get => GetValue(WindowStateProperty);
        set => SetValue(WindowStateProperty, value);
    }

    /// <summary>
    /// Enables or disables resizing of the window.
    /// </summary>
    public bool CanResize
    {
        get => GetValue(CanResizeProperty);
        set => SetValue(CanResizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the icon of the window.
    /// </summary>
    public WindowIcon? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the startup location of the window.
    /// </summary>
    public WindowStartupLocation WindowStartupLocation
    {
        get => GetValue(WindowStartupLocationProperty);
        set => SetValue(WindowStartupLocationProperty, value);
    }

    /// <summary>
    /// Gets or sets the window position in screen coordinates.
    /// </summary>
    public PixelPoint Position
    {
        get => GetValue(PositionProperty);
        set
        {
            SetValue(PositionProperty, value);
            Canvas.SetLeft(this, value.X);
            Canvas.SetTop(this, value.Y);
        }
    }

    /// <summary>
    /// Fired before a window is closed.
    /// </summary>
    public event EventHandler<WindowClosingEventArgs>? Closing;

    //    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    //{
    //    // Call the grandparent's MyMethod
    //    MethodInfo method = typeof(WindowBase).GetMethod("OnAttachedToVisualTree", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    //    method.Invoke(this, [e]);
    //}

    public void MaximizeWindow()
    {
        BringToTop();
        var parent = Parent as ManagedWindowsPanel;
        if (WindowState == WindowState.Normal)
        {
            _normalWidth = this.Width;
            _normalHeight = this.Height;
        }
        Canvas.SetLeft(this, 0);
        Canvas.SetTop(this, 0);
        this.Width = parent.Bounds.Width;
        this.Height = parent.Bounds.Height;
        WindowState = WindowState.Maximized;
        SetWindowStatePseudoClasses();
    }

    public void RestoreWindow()
    {
        BringToTop();
        WindowState = WindowState.Normal;
        Canvas.SetLeft(this, (int)this.Position.X);
        Canvas.SetTop(this, (int)this.Position.Y);
        this.Width = _normalWidth;
        this.Height = _normalHeight;
        SetWindowStatePseudoClasses();
    }

    public void MinimizeWindow()
    {
        BringToTop();
        var parent = Parent as ManagedWindowsPanel;
        if (WindowState == WindowState.Normal)
        {
            _normalWidth = this.Width;
            _normalHeight = this.Height;
        }
        WindowState = WindowState.Minimized;
        SetWindowStatePseudoClasses();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        var partTitleBar = e.NameScope.Find<Control>(PART_TitleBar);
        if (partTitleBar != null)
        {
            SetupTitleBar(partTitleBar);
        }

        var partMinimizeButton = e.NameScope.Find<Button>(PART_MinimizeButton);
        if (partMinimizeButton != null)
            partMinimizeButton.Click += OnMinimizeClick;

        var partMaximizeButton = e.NameScope.Find<Button>(PART_MaximizeButton);
        if (partMaximizeButton != null)
            partMaximizeButton.Click += OnMaximizeClick;

        var partRestoreButton = e.NameScope.Find<Button>(PART_RestoreButton);
        if (partRestoreButton != null)
            partRestoreButton.Click += OnRestoreClick;

        var partCloseButton = e.NameScope.Find<Button>(PART_CloseButton);
        if (partCloseButton != null)
            partCloseButton.Click += OnCloseClick;

        SetupResize(e.NameScope.Find<Border>(PART_ResizeBorder));

        SetWindowStatePseudoClasses();
    }

    private void SetupTitleBar(Control? partTitleBar)
    {
        if (partTitleBar == null)
            return;

        PixelPoint? start = null;
        var parent = this.Parent as Control;

        partTitleBar.PointerPressed += (object? sender, PointerPressedEventArgs e) =>
        {
            var properties = e.GetCurrentPoint(this).Properties;
            if (properties.IsLeftButtonPressed)
            {
                var point = e.GetPosition(parent);
                start = new PixelPoint((int)point.X, (int)point.Y);
                Debug.WriteLine($"start: {start.Value.X},{start.Value.Y} {Position.X},{Position.Y}");
                SetDraggingPseudoClasses(this, true);

                // AddAdorner(_draggedContainer);
                BringToTop();
            }
        };

        partTitleBar.PointerReleased += (object? sender, PointerReleasedEventArgs e) =>
        {
            if (start != null)
            {
                if (e.InitialPressMouseButton == MouseButton.Left)
                {
                    SetDraggingPseudoClasses(this, false);
                    start = null;
                }
            }
        };

        partTitleBar.PointerMoved += (object? sender, PointerEventArgs e) =>
        {
            var properties = e.GetCurrentPoint(this).Properties;
            if (start != null && properties.IsLeftButtonPressed)
            {
                var point = e.GetPosition(parent);
                var position = new PixelPoint((int)point.X, (int)point.Y);
                var delta = position - start.Value;
                start = position;
                this.Position = this.Position + delta;
            }
        };

        partTitleBar.PointerCaptureLost += (object? sender, PointerCaptureLostEventArgs e) =>
        {
            SetDraggingPseudoClasses(this, false);
            start = null;
        };

        partTitleBar.DoubleTapped += OnTitleBarDoubleClick;
    }

    private void SetDraggingPseudoClasses(Control control, bool isDragging)
    {
        if (isDragging)
        {
            ((IPseudoClasses)control.Classes).Add(":dragging");
        }
        else
        {
            ((IPseudoClasses)control.Classes).Remove(":dragging");
        }
    }

    private void SetWindowStatePseudoClasses()
    {
        var classes = ((IPseudoClasses)this.Classes);
        classes.Remove(":minimized");
        classes.Remove(":normal");
        classes.Remove(":maximized");
        switch (WindowState)
        {
            case WindowState.Minimized:
                classes.Add(":minimized");
                break;
            case WindowState.Maximized:
                classes.Add(":maximized");
                Canvas.SetLeft(this, 0);
                Canvas.SetTop(this, 0);
                break;
            case WindowState.Normal:
                classes.Add(":normal");
                Canvas.SetLeft(this, Position.X);
                Canvas.SetTop(this, Position.Y);
                break;
        }
    }
    private void BringToTop()
    {
        var canvas = Parent as Canvas;
        this.ZIndex = canvas.Children.Where(child => child is ManagedWindow).Max(child => (child as ManagedWindow).ZIndex) + 1;
    }

    void SetupResize(Border? border)
    {
        var borderWidth = 4;
        if (border == null)
            return;
        WindowEdge? edge = null;
        Point? start = null;
        border.PointerPressed += (i, e) =>
        {
            BringToTop();
            var properties = e.GetCurrentPoint(this).Properties;
            if (properties.IsLeftButtonPressed && this?.Parent is Control parent)
            {
                var point = e.GetPosition(this.Parent as Control);
                edge = GetEdge(border, borderWidth, point);
                border.Cursor = new Cursor(GetCursorForEdge(edge));
                if (edge != null)
                {
                    start = point;
                    e.Pointer.Capture(border);
                }
            }
        };
        border.PointerReleased += (i, e) =>
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                start = null;
                edge = null;
                e.Pointer.Capture(null);
                border.Cursor = new Cursor(GetCursorForEdge(edge));
            }
        };
        border.PointerCaptureLost += (i, e) =>
        {
            start = null;
            edge = null;
            e.Pointer.Capture(null);
            border.Cursor = new Cursor(GetCursorForEdge(edge));
        };
        border.PointerMoved += (i, e) =>
        {
            var position = e.GetPosition(this.Parent as Control);

            var properties = e.GetCurrentPoint(this).Properties;
            if (edge != null &&
                start != null &&
                properties.IsLeftButtonPressed)
            {
                double top = Canvas.GetTop(this);
                double left = Canvas.GetLeft(this);
                double width = this.Width;
                double height = this.Height;

                var deltaX = position.X - start.Value.X;
                var deltaY = position.Y - start.Value.Y;
                Debug.WriteLine($"{(int)start.Value.X} {(int)start.Value.Y} => {(int)position.X} {(int)position.Y} ");
                Debug.WriteLine("deltaX: " + (int)deltaX + " deltaY: " + (int)deltaY);
                switch (edge)
                {
                    case WindowEdge.West:
                        left += deltaX;
                        width -= deltaX;
                        break;
                    case WindowEdge.East:
                        width += deltaX;
                        break;
                    case WindowEdge.North:
                        top += deltaY;
                        height -= deltaY;
                        break;
                    case WindowEdge.South:
                        height += deltaY;
                        break;
                    case WindowEdge.NorthWest:
                        top += deltaY;
                        height -= deltaY;
                        left += deltaX;
                        width -= deltaX;
                        break;
                    case WindowEdge.NorthEast:
                        width += deltaX;
                        top += deltaY;
                        height -= deltaY;
                        break;
                    case WindowEdge.SouthWest:
                        left += deltaX;
                        width -= deltaX;
                        height += deltaY;
                        break;
                    case WindowEdge.SouthEast:
                        height += deltaY;
                        width += deltaX;
                        break;
                }
                if (left >= 0)
                    Canvas.SetLeft(this, left);
                if (top >= 0)
                    Canvas.SetTop(this, top);
                if (width != Width && width >= MinWidth)
                    Width = width;
                if (height != Height && height >= MinHeight)
                    Height = height;

                start = position;
            }
            else
            {
                var edgeTemp = GetEdge(border, borderWidth, position);
                border.Cursor = new Cursor(GetCursorForEdge(edgeTemp));
            }
        };
    }

    private WindowEdge? GetEdge(Control control, int borderWidth, Point? start)
    {
        if (start == null)
            return null;
        double top = Canvas.GetTop(this);
        double left = Canvas.GetLeft(this);
        double right = left + this.Width;
        double bottom = top + this.Height;

        //var top = Position.Y;
        //var bottom = Position.Y + Height;
        //var left = Position.X;
        //var right = Position.X + Width;
        var leftEdge = start.Value.X >= left &&
                       start.Value.X <= left + borderWidth;
        var rightEdge = start.Value.X >= right - borderWidth &&
                        start.Value.X <= right;
        var topEdge = start.Value.Y >= top &&
                        start.Value.Y <= top + borderWidth;
        var bottomEdge = start.Value.Y >= bottom - borderWidth &&
                        start.Value.Y <= bottom;
        if (topEdge && leftEdge)
            return WindowEdge.NorthWest;
        else if (topEdge && rightEdge)
            return WindowEdge.NorthEast;
        else if (bottomEdge && leftEdge)
            return WindowEdge.SouthWest;
        else if (bottomEdge && rightEdge)
            return WindowEdge.SouthEast;
        else if (topEdge)
            return WindowEdge.North;
        else if (bottomEdge)
            return WindowEdge.South;
        else if (leftEdge)
            return WindowEdge.West;
        else if (rightEdge)
            return WindowEdge.East;
        return null;
    }

    StandardCursorType GetCursorForEdge(WindowEdge? edge)
        => edge switch
        {
            WindowEdge.North => StandardCursorType.TopSide,
            WindowEdge.South => StandardCursorType.BottomSide,
            WindowEdge.West => StandardCursorType.LeftSide,
            WindowEdge.East => StandardCursorType.RightSide,
            WindowEdge.NorthWest => StandardCursorType.TopLeftCorner,
            WindowEdge.NorthEast => StandardCursorType.TopRightCorner,
            WindowEdge.SouthWest => StandardCursorType.BottomLeftCorner,
            WindowEdge.SouthEast => StandardCursorType.BottomRightCorner,
            _ => StandardCursorType.Arrow
        };

    private void OnTitleBarDoubleClick(object? sender, TappedEventArgs e)
    {
        if (WindowState == WindowState.Minimized)
            RestoreWindow();
        else if (WindowState == WindowState.Normal)
            MaximizeWindow();
        else if (WindowState == WindowState.Maximized)
            RestoreWindow();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        var ctor = typeof(WindowClosingEventArgs).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
            [typeof(WindowCloseReason), typeof(bool)]);
        WindowClosingEventArgs args = (WindowClosingEventArgs)ctor.Invoke([WindowCloseReason.WindowClosing, false]);
        Closing?.Invoke(this, args);
        if (!args.Cancel)
        {
            var windowsPanel = (ManagedWindowsPanel)this.Parent;
            windowsPanel.Children.Remove(this);
            RaiseEvent(new RoutedEventArgs(WindowClosedEvent));
        }
    }


    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        MinimizeWindow();
    }


    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        MaximizeWindow();
    }

    private void OnRestoreClick(object? sender, RoutedEventArgs e)
    {
        RestoreWindow();
    }

}