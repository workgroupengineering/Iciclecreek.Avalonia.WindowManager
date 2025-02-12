using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
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
[TemplatePart(PART_ResizeLeft, typeof(Control))]
[TemplatePart(PART_ResizeTop, typeof(Control))]
[TemplatePart(PART_ResizeRight, typeof(Control))]
[TemplatePart(PART_ResizeBottom, typeof(Control))]
[TemplatePart(PART_ResizeTopLeft, typeof(Control))]
[TemplatePart(PART_ResizeTopRight, typeof(Control))]
[TemplatePart(PART_ResizeBottomRight, typeof(Control))]
[TemplatePart(PART_ResizeBottomLeft, typeof(Control))]
[PseudoClasses(":minimized", ":maximized", ":fullscreen", ":dragging")]
public class ManagedWindow : ContentControl
{
    public const string PART_TitleBar = "PART_TitleBar";
    public const string PART_MinimizeButton = "PART_MinimizeButton";
    public const string PART_MaximizeButton = "PART_MaximizeButton";
    public const string PART_RestoreButton = "PART_RestoreButton";
    public const string PART_CloseButton = "PART_CloseButton";
    public const string PART_ResizeLeft = "PART_ResizeLeft";
    public const string PART_ResizeTop = "PART_ResizeTop";
    public const string PART_ResizeRight = "PART_ResizeRight";
    public const string PART_ResizeBottom = "PART_ResizeBottom";
    public const string PART_ResizeTopLeft = "PART_ResizeTopLeft";
    public const string PART_ResizeTopRight = "PART_ResizeTopRight";
    public const string PART_ResizeBottomRight = "PART_ResizeBottomRight";
    public const string PART_ResizeBottomLeft = "PART_ResizeBottomLeft";

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

    private bool _enableDrag;
    private Point _start;
    private Control? _parent;
    private Control? _draggedContainer;
    private bool _captured;

    public ManagedWindow()
    {
    }

    //    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    //{
    //    // Call the grandparent's MyMethod
    //    MethodInfo method = typeof(WindowBase).GetMethod("OnAttachedToVisualTree", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    //    method.Invoke(this, [e]);
    //}

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        var partTitleBar = e.NameScope.Find<Control>(PART_TitleBar);
        if (partTitleBar != null)
        {
            partTitleBar.PointerPressed += OnPointerPressed;
            partTitleBar.PointerMoved += OnPointerMoved;
            partTitleBar.PointerReleased += OnPointerReleased;
            partTitleBar.PointerCaptureLost += OnPointerCaptureLost;
            partTitleBar.DoubleTapped += OnTitleBarDoubleClick;
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
            partCloseButton.Click += OnClose;

        SetupSide(e.NameScope.Find<Control>(PART_ResizeLeft), StandardCursorType.LeftSide, WindowEdge.West);
        SetupSide(e.NameScope.Find<Control>(PART_ResizeRight), StandardCursorType.RightSide, WindowEdge.East);
        SetupSide(e.NameScope.Find<Control>(PART_ResizeTop), StandardCursorType.TopSide, WindowEdge.North);
        SetupSide(e.NameScope.Find<Control>(PART_ResizeBottom), StandardCursorType.BottomSide, WindowEdge.South);
        SetupSide(e.NameScope.Find<Control>(PART_ResizeTopLeft), StandardCursorType.TopLeftCorner, WindowEdge.NorthWest);
        SetupSide(e.NameScope.Find<Control>(PART_ResizeTopRight), StandardCursorType.TopRightCorner, WindowEdge.NorthEast);
        SetupSide(e.NameScope.Find<Control>(PART_ResizeBottomLeft), StandardCursorType.BottomLeftCorner, WindowEdge.SouthWest);
        SetupSide(e.NameScope.Find<Control>(PART_ResizeBottomRight), StandardCursorType.BottomRightCorner, WindowEdge.SouthEast);

        SetWindowStatePseudoClasses();
    }

    void SetupSide(Control? control, StandardCursorType cursor, WindowEdge edge)
    {
        if (control == null)
            return;
        control.Cursor = new Cursor(cursor);
        Point? start = null;
        control.PointerPressed += (i, e) =>
        {
            BringToTop();
            var properties = e.GetCurrentPoint(this).Properties;
            if (properties.IsLeftButtonPressed && this?.Parent is Control parent)
            {
                start = e.GetPosition(_parent);
            }
        };
        control.PointerReleased += (i, e) =>
        {
            if (start != null && e.InitialPressMouseButton == MouseButton.Left)
            {
                start = null;
            }
        };
        control.PointerCaptureLost += (i, e) =>
        {
            start = null;
        };
        control.PointerMoved += (i, e) =>
        {
            var properties = e.GetCurrentPoint(this).Properties;
            if (start != null && properties.IsLeftButtonPressed)
            {
                var position = e.GetPosition(_parent);
                double top = Canvas.GetTop(this);
                double left = Canvas.GetLeft(this);
                double width = this.Width;
                double height = this.Height;

                var deltaX = position.X - start.Value.X;
                var deltaY = position.Y - start.Value.Y;
                Debug.WriteLine($"{start.Value.X} {start.Value.Y} => {position.X} {position.Y} ");
                Debug.WriteLine("deltaX: " + deltaX + " deltaY: " + deltaY);
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
                if (width != Width && width >= MinWidth)
                    Width = width;
                if (height != Height && height >= MinHeight)
                    Height = height;
                if (left >= 0)
                    Canvas.SetLeft(this, left);
                if (top >= 0)
                    Canvas.SetTop(this, top);
                start = position;
            }
        };
    }

    private void OnTitleBarDoubleClick(object? sender, TappedEventArgs e)
    {
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
        else if (WindowState == WindowState.Normal)
            WindowState = WindowState.Maximized;
        else if (WindowState == WindowState.Maximized)
            WindowState = WindowState.Normal;
        SetWindowStatePseudoClasses();
        BringToTop();
    }

    private void OnClose(object? sender, RoutedEventArgs e)
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
        WindowState = WindowState.Minimized;
        SetWindowStatePseudoClasses();
    }


    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Maximized;
        SetWindowStatePseudoClasses();
        BringToTop();
    }

    private void OnRestoreClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Normal;
        SetWindowStatePseudoClasses();
        BringToTop();
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
    /// Fired before a window is closed.
    /// </summary>
    public event EventHandler<WindowClosingEventArgs>? Closing;


    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;
        if (properties.IsLeftButtonPressed
            && this?.Parent is Control parent)
        {
            _enableDrag = true;
            _start = e.GetPosition(parent);
            _parent = parent;
            _draggedContainer = this;

            SetDraggingPseudoClasses(_draggedContainer, true);

            // AddAdorner(_draggedContainer);
            BringToTop();

            _captured = true;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_captured)
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                Released();
            }

            _captured = false;
        }
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        Released();
        _captured = false;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;
        if (_captured
            && properties.IsLeftButtonPressed)
        {
            if (_parent is null || _draggedContainer is null || !_enableDrag)
            {
                return;
            }

            var position = e.GetPosition(_parent);
            var deltaX = position.X - _start.X;
            var deltaY = position.Y - _start.Y;
            _start = position;
            var left = Canvas.GetLeft(_draggedContainer);
            var top = Canvas.GetTop(_draggedContainer);
            Canvas.SetLeft(_draggedContainer, left + deltaX);
            Canvas.SetTop(_draggedContainer, top + deltaY);
        }
    }



    private void Released()
    {
        if (_enableDrag)
        {
            if (_parent is not null && _draggedContainer is not null)
            {
                // RemoveAdorner(_draggedContainer);
            }

            if (_draggedContainer is not null)
            {
                SetDraggingPseudoClasses(_draggedContainer, false);
            }

            _enableDrag = false;
            _parent = null;
            _draggedContainer = null;
        }
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
                break;
            case WindowState.Normal:
                classes.Add(":normal");
                break;
        }
    }
    private void BringToTop()
    {
        var canvas = Parent as Canvas;
        this.ZIndex = canvas.Children.Where(child => child is ManagedWindow).Max(child => (child as ManagedWindow).ZIndex) + 1;
    }



}