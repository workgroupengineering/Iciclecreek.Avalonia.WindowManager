using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Iciclecreek.Avalonia.WindowManager;

[TemplatePart(PART_TitleBar, typeof(Control))]
[TemplatePart(PART_MinimizeButton, typeof(Button))]
[TemplatePart(PART_MaximizeButton, typeof(Button))]
[TemplatePart(PART_RestoreButton, typeof(Button))]
[TemplatePart(PART_CloseButton, typeof(Button))]
[TemplatePart(PART_ResizeBorder, typeof(Border))]
[PseudoClasses(":minimized", ":maximized", ":normal", ":dragging", ":active")]
public class ManagedWindow : ContentControl
{
    public const string PART_TitleBar = "PART_TitleBar";
    public const string PART_MinimizeButton = "PART_MinimizeButton";
    public const string PART_MaximizeButton = "PART_MaximizeButton";
    public const string PART_RestoreButton = "PART_RestoreButton";
    public const string PART_CloseButton = "PART_CloseButton";
    public const string PART_ResizeBorder = "PART_ResizeBorder";

    private double _normalWidth;
    private double _normalHeight;
    private BoxShadows _normalBoxShadow;
    private Thickness _normalMargin;
    private Border? _windowBorder;
    private ManagedWindow? _owner;
    private bool _isActive;
    private bool _showingAsDialog;
    private object? _dialogResult;
    private readonly List<(ManagedWindow Child, bool IsDialog)> _children = new List<(ManagedWindow, bool)>();

    /// <summary>
    /// Defines the <see cref="IsActive"/> property.
    /// </summary>
    public static readonly DirectProperty<ManagedWindow, bool> IsActiveProperty =
        AvaloniaProperty.RegisterDirect<ManagedWindow, bool>(nameof(IsActive), o => o.IsActive);

    /// <summary>
    /// Defines the <see cref="Owner"/> property.
    /// </summary>
    public static readonly DirectProperty<ManagedWindow, ManagedWindow?> OwnerProperty =
        AvaloniaProperty.RegisterDirect<ManagedWindow, ManagedWindow?>(nameof(Owner), o => o.Owner);

    public static readonly StyledProperty<bool> TopmostProperty =
        AvaloniaProperty.Register<ManagedWindow, bool>(nameof(Topmost));

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
    /// Defines the <see cref="ClosingBehavior"/> property.
    /// </summary>
    public static readonly StyledProperty<WindowClosingBehavior> ClosingBehaviorProperty =
        AvaloniaProperty.Register<Window, WindowClosingBehavior>(nameof(ClosingBehavior));

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

    /// <summary>
    /// Defines the <see cref="BoxShadow"/> property.
    /// </summary>
    public static readonly StyledProperty<BoxShadows> BoxShadowProperty =
        AvaloniaProperty.Register<ManagedWindow, BoxShadows>(nameof(BoxShadow));


    static ManagedWindow()
    {
        //var theme = new ManagedWindowControlTheme() { TargetType = typeof(ManagedWindow) };
        //Control.ThemeProperty.OverrideDefaultValue<ManagedWindow>(theme);
    }

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

    /// <summary>
    /// Gets or sets the box shadow effect parameters
    /// </summary>
    public BoxShadows BoxShadow
    {
        get => GetValue(BoxShadowProperty);
        set => SetValue(BoxShadowProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating how the <see cref="Closing"/> event behaves in the presence
    /// of child windows.
    /// </summary>
    public WindowClosingBehavior ClosingBehavior
    {
        get => GetValue(ClosingBehaviorProperty);
        set => SetValue(ClosingBehaviorProperty, value);
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
    /// Gets or sets the owner of the window.
    /// </summary>
    public ManagedWindow? Owner
    {
        get => _owner;
        protected set => SetAndRaise(OwnerProperty, ref _owner, value);
    }

    /// <summary>
    /// Gets or sets whether this window appears on top of all other windows
    /// </summary>
    public bool Topmost
    {
        get => GetValue(TopmostProperty);
        set => SetValue(TopmostProperty, value);
    }

    /// <summary>
    /// Gets a value that indicates whether the window is active.
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        private set => SetAndRaise(IsActiveProperty, ref _isActive, value);
    }

    public ITopLevelImpl PlatformImpl => TopLevel.GetTopLevel(this)!.PlatformImpl!;

    /// <summary>
    /// File System storage service used for file pickers and bookmarks.
    /// </summary>
    public IStorageProvider StorageProvider => TopLevel.GetTopLevel(this)!.StorageProvider;

    public IInsetsManager? InsetsManager => TopLevel.GetTopLevel(this)!.PlatformImpl!.TryGetFeature<IInsetsManager>();
    public IInputPane? InputPane => PlatformImpl!.TryGetFeature<IInputPane>();
    public ILauncher Launcher => PlatformImpl!.TryGetFeature<ILauncher>();

    /// <summary>
    /// Gets the platform's clipboard implementation
    /// </summary>
    public IClipboard? Clipboard => PlatformImpl!.TryGetFeature<IClipboard>();

    /// <inheritdoc />
    public IFocusManager? FocusManager => TopLevel.GetTopLevel(this)!.FocusManager;

    /// <summary>
    /// Gets a collection of child windows owned by this window.
    /// </summary>
    public IReadOnlyList<ManagedWindow> OwnedWindows => _children.Select(x => x.Child).ToArray();

    /// <summary>
    /// Fired when the window is activated.
    /// </summary>
    public event EventHandler? Activated;

    /// <summary>
    /// Fired when the window is deactivated.
    /// </summary>
    public event EventHandler? Deactivated;

    /// <summary>
    /// Fired when the window is opened.
    /// </summary>
    public event EventHandler? Opened;

    /// <summary>
    /// Fired when the window is closed.
    /// </summary>
    public event EventHandler? Closed;

    /// <summary>
    /// Fired when the window position is changed.
    /// </summary>
    public event EventHandler<PixelPointEventArgs>? PositionChanged;

    /// <summary>
    /// Occurs when the window is resized.
    /// </summary>
    /// <remarks>
    /// Although this event is similar to the <see cref="Control.SizeChanged"/> event, they are
    /// conceptually different:
    /// 
    /// - <see cref="Resized"/> is a window-level event, fired when a resize notification arrives
    ///   from the platform windowing subsystem. The event args contain details of the source of
    ///   the resize event in the <see cref="WindowResizedEventArgs.Reason"/> property. This
    ///   event is raised before layout has been run on the window's content.
    /// - <see cref="Control.SizeChanged"/> is a layout-level event, fired when a layout pass
    ///   completes on a control. <see cref="Control.SizeChanged"/> is present on all controls
    ///   and is fired when the control's size changes for any reason, including a
    ///   <see cref="Resized"/> event in the case of a Window.
    /// </remarks>
    public event EventHandler<WindowResizedEventArgs>? Resized;

    /// <summary>
    /// Fired before a window is closed.
    /// </summary>
    public event EventHandler<WindowClosingEventArgs>? Closing;

    public void MaximizeWindow()
    {
        BringToTop();
        var parent = (WindowManagerPanel)Parent!;
        if (WindowState == WindowState.Normal)
        {
            _normalWidth = this.Width;
            _normalHeight = this.Height;
            if (_windowBorder != null)
            {
                _normalBoxShadow = _windowBorder.BoxShadow!;
                _normalMargin = _windowBorder.Margin;
            }
        }
        Canvas.SetLeft(this, 0);
        Canvas.SetTop(this, 0);
        this.Width = parent.Bounds.Width;
        this.Height = parent.Bounds.Height;
        if (_windowBorder != null)
        {
            _windowBorder.Margin = new Thickness(0);
            _windowBorder.BoxShadow = new BoxShadows();
        }
        WindowState = WindowState.Maximized;
        SetPsuedoClasses();
    }

    public void RestoreWindow()
    {
        BringToTop();
        WindowState = WindowState.Normal;
        Canvas.SetLeft(this, (int)this.Position.X);
        Canvas.SetTop(this, (int)this.Position.Y);
        this.Width = _normalWidth;
        this.Height = _normalHeight;
        if (_windowBorder != null)
        {

            _windowBorder.Margin = _normalMargin;
            _windowBorder.BoxShadow = _normalBoxShadow;
        }
        SetPsuedoClasses();
    }

    public void MinimizeWindow()
    {
        BringToTop();
        if (WindowState == WindowState.Normal)
        {
            _normalWidth = this.Width;
            _normalHeight = this.Height;
        }
        WindowState = WindowState.Minimized;
        SetPsuedoClasses();
    }

    /// <summary>
    /// Activates the window.
    /// </summary>
    public void Activate()
    {
        if (!IsActive)
        {
            if (WindowState == WindowState.Minimized)
            {
                RestoreWindow();
            }

            OnActivated();
            IsActive = true;

            var windowsManager = this.FindAncestorOfType<WindowManagerPanel>();
            if (windowsManager != null)
            {
                foreach (var win in windowsManager.Windows.Where(win => win != this))
                {
                    win.Deactivate();
                }
            }
            BringToTop();
            SetPsuedoClasses();
        }
    }

    /// <summary>
    /// Deactivates the window
    /// </summary>
    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            OnDeactivated();
            SetPsuedoClasses();
        }
    }

    /// <summary>
    /// Hides the popup.
    /// </summary>
    public virtual void Hide()
    {
        IsVisible = false;
    }

    /// <summary>
    /// Shows the window.
    /// </summary>
    public virtual void Show()
    {
        IsVisible = true;

        OnOpened(EventArgs.Empty);
        if (ShowActivated)
        {
            Activate();
        }
    }

    /// <summary>
    /// Shows the window as a dialog.
    /// </summary>
    /// <param name="owner">The dialog's owner window.</param>
    /// <exception cref="InvalidOperationException">
    /// The window has already been closed.
    /// </exception>
    /// <returns>
    /// A task that can be used to track the lifetime of the dialog.
    /// </returns>
    public Task ShowDialog(ManagedWindow owner)
    {
        return ShowDialog<object>(owner);
    }

    /// <summary>
    /// Shows the window as a dialog.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type of the result produced by the dialog.
    /// </typeparam>
    /// <param name="owner">The dialog's owner window.</param>
    /// <returns>.
    /// A task that can be used to retrieve the result of the dialog when it closes.
    /// </returns>
    public Task<TResult> ShowDialog<TResult>(ManagedWindow owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        RaiseEvent(new RoutedEventArgs(WindowOpenedEvent));

        // EnsureInitialized();
        ApplyStyling();
        // _shown = true;
        _showingAsDialog = true;
        IsVisible = true;

        // SetWindowStartupLocation(owner);

        // _canHandleResized = true;

        //var initialSize = new Size(
        //    double.IsNaN(Width) ? ClientSize.Width : Width,
        //    double.IsNaN(Height) ? ClientSize.Height : Height);

        //if (initialSize != ClientSize)
        //{
        //    PlatformImpl?.Resize(initialSize, WindowResizeReason.Layout);
        //}

        //LayoutManager.ExecuteInitialLayoutPass();

        var result = new TaskCompletionSource<TResult>();

        Owner = owner;

        // Second call will calculate correct position because both current and owner windows have correct scaling.
        //            SetWindowStartupLocation(owner);

        //StartRendering();
        //PlatformImpl?.Show(ShowActivated, true);
        this.Closed += (sender, e) =>
        {
            owner.Activate();
            result.SetResult((TResult)(_dialogResult ?? default(TResult)!));
        };

        OnOpened(EventArgs.Empty);
        return result.Task;
    }

    /// <summary>
    /// Closes the window.
    /// </summary>
    public void Close()
    {
        CloseCore(WindowCloseReason.WindowClosing, true, false);
    }

    /// <summary>
    /// Closes a dialog window with the specified result.
    /// </summary>
    /// <param name="dialogResult">The dialog result.</param>
    /// <remarks>
    /// When the window is shown with the <see cref="ShowDialog{TResult}(Window)"/>
    /// or <see cref="ShowDialog{TResult}(Window)"/> method, the
    /// resulting task will produce the <see cref="_dialogResult"/> value when the window
    /// is closed.
    /// </remarks>
    public void Close(object? dialogResult)
    {
        _dialogResult = dialogResult;
        CloseCore(WindowCloseReason.WindowClosing, true, false);
    }

    internal void CloseCore(WindowCloseReason reason, bool isProgrammatic, bool ignoreCancel)
    {
        bool close = true;

        try
        {
            var args = ActivatorEx.CreateInstance<WindowClosingEventArgs>(reason, isProgrammatic);
            if (ShouldCancelClose(args))
            {
                close = false;
            }
        }
        finally
        {
            if (close || ignoreCancel)
            {
                CloseInternal();
            }
        }
    }

    private void CloseInternal()
    {
        foreach (var (child, _) in _children.ToArray())
        {
            child.CloseInternal();
        }


        _showingAsDialog = false;

        Owner = null;
    }

    private bool ShouldCancelClose(WindowClosingEventArgs args)
    {
        switch (ClosingBehavior)
        {
            case WindowClosingBehavior.OwnerAndChildWindows:
                bool canClose = true;

                if (_children.Count > 0)
                {
                    var childArgs = args.CloseReason == WindowCloseReason.WindowClosing ?
                        ActivatorEx.CreateInstance<WindowClosingEventArgs>(WindowCloseReason.OwnerWindowClosing, args.IsProgrammatic) :
                        args;

                    foreach (var (child, _) in _children.ToArray())
                    {
                        if (child.ShouldCancelClose(childArgs))
                        {
                            canClose = false;
                        }
                    }
                }

                if (canClose)
                {
                    OnClosing(args);

                    return args.Cancel;
                }

                return true;
            case WindowClosingBehavior.OwnerWindowOnly:
                OnClosing(args);

                return args.Cancel;
        }

        return false;
    }

    /// <summary>
    /// Raies the <see cref="Activated"/> event.
    /// </summary>
    protected virtual void OnActivated()
    {
        Dispatcher.UIThread.Post(() => Activated?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>
    /// Raises the <see cref="Deactivated"/> event.
    /// </summary>
    protected virtual void OnDeactivated()
    {
        Dispatcher.UIThread.Post(() => Deactivated?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>
    /// Raises the <see cref="Opened"/> event.
    /// </summary>
    /// <param name="e">The event args.</param>
    protected virtual void OnOpened(EventArgs e)
    {
        Dispatcher.UIThread.Post(() => Opened?.Invoke(this, e));
    }


    /// <summary>
    /// Raises the <see cref="Closing"/> event.
    /// </summary>
    /// <param name="e">The event args.</param>
    /// <remarks>
    /// A type that derives from <see cref="Window"/>  may override <see cref="OnClosing"/>. The
    /// overridden method must call <see cref="OnClosing"/> on the base class if the
    /// <see cref="Closing"/> event needs to be raised.
    /// </remarks>
    protected virtual void OnClosing(WindowClosingEventArgs e)
    {
        Dispatcher.UIThread.Post(() => Closing?.Invoke(this, e));
    }

    /// <summary>
    /// Raises the <see cref="Closed"/> event.
    /// </summary>
    /// <param name="e">The event args.</param>
    protected virtual void OnClosed(EventArgs e)
    {
        Dispatcher.UIThread.Post(() => Closed?.Invoke(this, e));
    }


    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        this.GotFocus += (s, e) => Activate();
        this.LostFocus += (s, e) => Deactivate();

        //if (this.Theme == null)
        //    this.Theme = (ControlTheme)this.FindResource("ManagedWindow");

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

        _windowBorder = e.NameScope.Find<Border>(PART_ResizeBorder);
        SetupResize(_windowBorder);

        this.Tapped += ManagedWindow_Tapped;
        SetPsuedoClasses();
    }

    private void ManagedWindow_Tapped(object? sender, TappedEventArgs e)
    {
        if (!IsActive)
        {
            Activate();
        }
    }

    private void SetupTitleBar(Control? partTitleBar)
    {
        if (partTitleBar == null)
            return;

        PixelPoint? start = null;
        var parent = this.Parent as Control;

        partTitleBar.PointerPressed += (object? sender, PointerPressedEventArgs e) =>
        {
            if (!IsActive)
                Activate();

            if (WindowState == WindowState.Normal)
            {

                var properties = e.GetCurrentPoint(this).Properties;
                if (properties.IsLeftButtonPressed)
                {
                    var point = e.GetPosition(parent);
                    start = new PixelPoint((int)point.X, (int)point.Y);
                    SetPsuedoClasses(true);
                }
            }
        };

        partTitleBar.PointerReleased += (object? sender, PointerReleasedEventArgs e) =>
        {
            if (start != null)
            {
                if (e.InitialPressMouseButton == MouseButton.Left)
                {
                    SetPsuedoClasses(false);
                    start = null;
                }
            }
        };

        partTitleBar.PointerMoved += (object? sender, PointerEventArgs e) =>
        {
            if (WindowState == WindowState.Normal)
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
            }
        };

        partTitleBar.PointerCaptureLost += (object? sender, PointerCaptureLostEventArgs e) =>
        {
            SetPsuedoClasses(false);
            start = null;
        };

        partTitleBar.DoubleTapped += OnTitleBarDoubleClick;
    }

    private void SetPsuedoClasses(bool isDragging = false)
    {
        var classes = ((IPseudoClasses)this.Classes);
        if (isDragging)
        {
            classes.Add(":dragging");
        }
        else
        {
            classes.Remove(":dragging");
        }
        if (IsActive)
        {
            classes.Add(":active");
        }
        else
        {
            classes.Remove(":active");
        }
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
    public void BringToTop()
    {
        var windowsPanel = this.FindAncestorOfType<WindowManagerPanel>()!;
        windowsPanel.BringToTop(this);
    }

    void SetupResize(Border? border)
    {
        if (border == null)
            return;

        // when there is a box shadow we need to adjust the margin to allow it to be rendered.
        if (border.BoxShadow.Count > 0)
            border.Margin = new Thickness(0, 0, border.BoxShadow[0].OffsetX, border.BoxShadow[0].OffsetY);

        WindowEdge? edge = null;
        Point? start = null;
        border.PointerPressed += (i, e) =>
        {
            if (!IsActive)
                Activate();

            if (CanResize && WindowState == WindowState.Normal)
            {
                var properties = e.GetCurrentPoint(this).Properties;
                if (properties.IsLeftButtonPressed && this?.Parent is Control parent)
                {
                    var point = e.GetPosition(this.Parent as Control);
                    edge = GetEdge(border, point);
                    border.Cursor = new Cursor(GetCursorForEdge(edge));
                    if (edge != null)
                    {
                        start = point;
                        e.Pointer.Capture(border);
                    }
                }
            }
        };
        border.PointerReleased += (i, e) =>
        {
            if (CanResize && e.InitialPressMouseButton == MouseButton.Left)
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
            if (CanResize)
            {
                border.Cursor = new Cursor(GetCursorForEdge(edge));
            }
        };
        border.PointerMoved += (i, e) =>
        {
            if (CanResize && WindowState == WindowState.Normal)
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

                    var deltaX = (int)position.X - (int)start.Value.X;
                    var deltaY = (int)position.Y - (int)start.Value.Y;

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

                    if (deltaX != 0 || deltaY != 0)
                    {
                        this.Position = new PixelPoint((int)left, (int)top);
                        if (width != Width && width >= MinWidth && width < MaxWidth)
                            Width = width;
                        if (height != Height && height >= MinHeight && height < MaxHeight)
                            Height = height;

                        start = position;
                    }
                }
                else
                {
                    var edgeTemp = GetEdge(border, position);
                    border.Cursor = new Cursor(GetCursorForEdge(edgeTemp));
                }
            }
        };
    }

    private WindowEdge? GetEdge(Border border, Point? start)
    {
        if (start == null)
            return null;
        double top = Canvas.GetTop(this);
        double left = Canvas.GetLeft(this);
        double right = left + this.Width;
        double bottom = top + this.Height;

        var leftEdge = start.Value.X >= left &&
                       start.Value.X <= left + border.BorderThickness.Left - border.Margin.Left;
        var rightEdge = start.Value.X >= right - border.BorderThickness.Right - border.Margin.Right &&
                        start.Value.X <= right;
        var topEdge = start.Value.Y >= top &&
                        start.Value.Y <= top + border.BorderThickness.Top - border.Margin.Top;
        var bottomEdge = start.Value.Y >= bottom - border.BorderThickness.Bottom - border.Margin.Bottom &&
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
        WindowClosingEventArgs? args = ActivatorEx.CreateInstance<WindowClosingEventArgs>(WindowCloseReason.WindowClosing, false);
        Closing?.Invoke(this, args);
        if (!args.Cancel)
        {
            Closed?.Invoke(this, new EventArgs());
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
