using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Presenters;
using Avalonia.Styling;
using Avalonia.Animation;
using Avalonia.VisualTree;
using Avalonia.LogicalTree;
using Consolonia.Controls;
using Avalonia.Metadata;

namespace Iciclecreek.Avalonia.WindowManager;

[TemplatePart(PART_TitleBar, typeof(Control))]
[TemplatePart(PART_Title, typeof(TextBlock))]
[TemplatePart(PART_MinimizeButton, typeof(Button))]
[TemplatePart(PART_MaximizeButton, typeof(Button))]
[TemplatePart(PART_RestoreButton, typeof(Button))]
[TemplatePart(PART_CloseButton, typeof(Button))]
[TemplatePart(PART_WindowBorder, typeof(Border))]
[TemplatePart(PART_ContentPresenter, typeof(Control))]
[PseudoClasses(":minimized", ":maximized", ":normal", ":dragging", ":active", ":hasmodal", ":ismodal", ":noborder")]
public class ManagedWindow : ContentControl
{
    public const string PART_ContentPresenter = "PART_ContentPresenter";
    public const string PART_TitleBar = "PART_TitleBar";
    public const string PART_Title = "PART_Title";
    public const string PART_MinimizeButton = "PART_MinimizeButton";
    public const string PART_MaximizeButton = "PART_MaximizeButton";
    public const string PART_RestoreButton = "PART_RestoreButton";
    public const string PART_CloseButton = "PART_CloseButton";
    public const string PART_WindowBorder = "PART_WindowBorder";

    private PixelPoint _minimizedPosition = new PixelPoint(int.MinValue, int.MinValue);
    private Rect _normalRect;
    private BoxShadows _normalBoxShadow;
    private Thickness _normalMargin;
    private ContentPresenter? _content;
    private Border? _windowBorder;
    private ManagedWindow? _owner;
    private bool _isActive;
    private object? _dialogResult;
    private Control _title;
    private Control _titleBar;
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

    /// <summary>
    /// Defines the <see cref="AnimateWindow"/> property. If True, animations will be used for transitions.
    /// </summary>
    public static readonly StyledProperty<bool> AnimateWindowProperty =
        AvaloniaProperty.Register<ManagedWindow, bool>(nameof(AnimateWindow), true);

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
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<ManagedWindow, object?>(nameof(Icon));

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
    /// Defines the <see cref="BoxShadow"/> property.
    /// </summary>
    public static readonly StyledProperty<BoxShadows> BoxShadowProperty =
        AvaloniaProperty.Register<ManagedWindow, BoxShadows>(nameof(BoxShadow));

    /// <summary>
    /// Defines the <see cref="SystemDecorations"/> property.
    /// </summary>
    public static readonly StyledProperty<SystemDecorations> SystemDecorationsProperty =
        AvaloniaProperty.Register<ManagedWindow, SystemDecorations>(nameof(SystemDecorations), SystemDecorations.Full);

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


    static ManagedWindow()
    {
        //AffectsRender<ManagedWindow>(
        //    BackgroundProperty,
        //    BorderBrushProperty,
        //    BorderTknessProperty,
        //    CornerRadiusProperty,
        //    BoxShadowProperty);
        //AffectsMeasure<ManagedWindow>(BorderThicknessProperty);
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

    /// <summary>
    /// Gets or sets value indicating that the window should animate when opening, closing, and resizing
    /// </summary>
    public bool AnimateWindow
    {
        get => GetValue(AnimateWindowProperty);
        set => SetValue(AnimateWindowProperty, value);
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
    [DependsOn(nameof(ContentTemplate))]
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Sets the system decorations (title bar, border, etc)
    /// </summary>
    public SystemDecorations SystemDecorations
    {
        get => GetValue(SystemDecorationsProperty);
        set => SetValue(SystemDecorationsProperty, value);
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
    /// Gets or sets the client size of the window.
    /// </summary>
    public Size ClientSize
    {
        get => new Size(this._content.Width, this._content.Height);
        protected set
        {
            this._content.Width = value.Width;
            this._content.Height = value.Height;
        } // TODO ? (this._content.DesiredSize, value);
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

    public Screens Screens => TopLevel.GetTopLevel(this)!.Screens!;

    public IInsetsManager? InsetsManager => TopLevel.GetTopLevel(this)!.PlatformImpl!.TryGetFeature<IInsetsManager>();
    public IInputPane? InputPane => PlatformImpl!.TryGetFeature<IInputPane>();
    public ILauncher Launcher => PlatformImpl!.TryGetFeature<ILauncher>();

    /// <summary>
    /// Gets the platform's clipboard implementation
    /// </summary>
    public IClipboard? Clipboard => PlatformImpl!.TryGetFeature<IClipboard>();

    /// <inheritdoc />
    public IFocusManager? FocusManager => TopLevel.GetTopLevel(this)!.FocusManager;

    public WindowsPanel? WindowManager => this.FindAncestorOfType<WindowsPanel>();

    private ManagedWindow? _modalDialog;
    public ManagedWindow? ModalDialog
    {
        get => _modalDialog;
        set
        {
            if (_modalDialog != null)
                throw new NotSupportedException("Already showing a modal dialog for this window");

            _modalDialog = value;
            if (_modalDialog != null)
            {
                _modalDialog.Closed += (sender, e) =>
                {
                    // when dialog closes change focus back to owner.
                    _modalDialog = null;
                    Activate();
                };
            }

            SetPsuedoClasses();
        }
    }

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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        switch (change.Property.Name)
        {
            case nameof(WindowState):
                if (WindowManager != null)
                {
                    if (change.OldValue is WindowState oldState)
                    {
                        CaptureWindowState(oldState);

                        switch (WindowState)
                        {
                            case WindowState.FullScreen:
                                OnFullscreenWindow();
                                break;
                            case WindowState.Maximized:
                                OnMaximizeWindow();
                                break;
                            case WindowState.Minimized:
                                OnMinimizeWindow();
                                break;
                            case WindowState.Normal:
                                OnNormalWindow();
                                break;
                            default:
                                break;
                        }
                    }
                }
                break;

            case nameof(SystemDecorations):
                SetPsuedoClasses();
                break;

            default:
                base.OnPropertyChanged(change);
                break;
        }
    }

    private void CaptureWindowState(WindowState state)
    {
        switch (state)
        {
            case WindowState.Normal:
                var width = double.IsNaN(this.Width) ? this.Bounds.Width : this.Width;
                var height = double.IsNaN(this.Height) ? this.Bounds.Height : this.Height;
                _normalRect = new Rect((int)this.Position.X, (int)this.Position.Y, (int)width, (int)height);
                if (_windowBorder != null)
                {
                    _normalBoxShadow = _windowBorder.BoxShadow!;
                    _normalMargin = _windowBorder.Margin;
                }
                break;
            case WindowState.Minimized:
                _minimizedPosition = new PixelPoint(this.Position.X, this.Position.Y);
                break;
            case WindowState.FullScreen:
            case WindowState.Maximized:
                if (_normalRect.Width == 0 && _normalRect.Height == 0)
                {
                    _normalRect = new Rect((int)2, (int)2, (int)WindowManager.Bounds.Width - 4, (int)WindowManager.Bounds.Height - 4);
                    if (_windowBorder != null)
                    {
                        _normalBoxShadow = _windowBorder.BoxShadow!;
                        _normalMargin = _windowBorder.Margin;
                    }
                }
                break;
            default:
                break;
        }
    }

    protected virtual async void OnMaximizeWindow()
    {
        BringToTop();

        SetPsuedoClasses();

        ArgumentNullException.ThrowIfNull(WindowManager, nameof(WindowsPanel));

        await ResizeAnimation(new Rect(this.Position.X, this.Position.Y, this.Bounds.Width, this.Bounds.Height),
                              new Rect(0, 0, WindowManager.Bounds.Width, WindowManager.Bounds.Height));

        this.Position = new PixelPoint((ushort)0, (ushort)0);
        this.Width = WindowManager.Bounds.Width;
        this.Height = WindowManager.Bounds.Height;
        if (_windowBorder != null)
        {
            _windowBorder.Margin = new Thickness(0);
            _windowBorder.BoxShadow = new BoxShadows();
        }
    }

    protected virtual async void OnFullscreenWindow()
    {
        throw new NotSupportedException();
    }

    protected virtual async void OnNormalWindow()
    {
        BringToTop();

        SetPsuedoClasses();

        await ResizeAnimation(new Rect(this.Position.X, this.Position.Y, this.Bounds.Width, this.Bounds.Height),
                              _normalRect);

        this.Position = new PixelPoint((int)_normalRect.Position.X, (int)_normalRect.Position.Y);
        this.Width = _normalRect.Width;
        this.Height = _normalRect.Height;

        if (_windowBorder != null)
        {
            _windowBorder.Margin = _normalMargin;
            _windowBorder.BoxShadow = _normalBoxShadow;
        }
    }

    protected virtual async void OnMinimizeWindow()
    {
        BringToTop();

        SetPsuedoClasses();

        if (_minimizedPosition.X == int.MinValue && _minimizedPosition.Y == int.MinValue)
            _minimizedPosition = new PixelPoint(this.Position.X, this.Position.Y);

        await ResizeAnimation(new Rect(this.Position.X, this.Position.Y, this.Bounds.Width, this.Bounds.Height),
                              new Rect(_minimizedPosition.X, _minimizedPosition.Y, _title.Bounds.Width, _title.Bounds.Height));

        this.Position = _minimizedPosition;
        this.Width = double.NaN;
        this.Height = double.NaN;
    }

    /// <summary>
    /// Activates the window.
    /// </summary>
    public void Activate()
    {
        if (!IsActive && ModalDialog == null && Parent != null)
        {
            if (WindowState == WindowState.Minimized)
            {
                return;
            }

            OnActivated();
            IsActive = true;

            foreach (var win in WindowManager.Windows.Where(win => win != this))
            {
                win.Deactivate();
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

    private bool _shown;
    /// <summary>
    /// Shows the window.
    /// </summary>
    public virtual void Show()
    {
        IsVisible = true;

        if (!_shown)
        {
            if (WindowManager == null)
                throw new Exception("ManagedWindow must be a child of WindowsPanel. Call WindowsPanel.ShowWindow(window) to show the window.");

            _shown = true;
            CaptureWindowState(WindowState);

            switch (WindowState)
            {
                case WindowState.Normal:
                    OnNormalWindow();
                    break;
                case WindowState.Maximized:
                    OnMaximizeWindow();
                    break;
                case WindowState.Minimized:
                    OnMinimizeWindow();
                    break;
                case WindowState.FullScreen:
                    OnFullscreenWindow();
                    break;
            }

            RaiseEvent(new RoutedEventArgs(WindowOpenedEvent));

            OnOpened(EventArgs.Empty);

            if (ShowActivated)
            {
                Activate();
            }
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
        if (ModalDialog != null)
            throw new NotSupportedException("Already showing a modal dialog for this window");

        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        this.Owner = owner;

        owner.ModalDialog = this;

        var result = new TaskCompletionSource<TResult>();

        this.Closed += (sender, e) =>
        {
            this.ModalDialog = null;
            result.SetResult((TResult)(_dialogResult ?? default(TResult)!));
        };

        owner.WindowManager.AddWindow(this);

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

    private async void CloseInternal()
    {
        foreach (var (child, _) in _children.ToArray())
        {
            child.CloseInternal();
        }

        await CloseAnimation();

        Owner = null;
        OnClosed(new EventArgs());
        RaiseEvent(new RoutedEventArgs(WindowClosedEvent));
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
        Dispatcher.UIThread.Invoke(() => Activated?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>
    /// Raises the <see cref="Deactivated"/> event.
    /// </summary>
    protected virtual void OnDeactivated()
    {
        Dispatcher.UIThread.Invoke(() => Deactivated?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>
    /// Raises the <see cref="Opened"/> event.
    /// </summary>
    /// <param name="e">The event args.</param>
    protected async virtual void OnOpened(EventArgs e)
    {
        await ShowAnimation();

        Dispatcher.UIThread.Invoke(() => Opened?.Invoke(this, e));
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
        Dispatcher.UIThread.Invoke(() => Closing?.Invoke(this, e));
    }

    /// <summary>
    /// Raises the <see cref="Closed"/> event.
    /// </summary>
    /// <param name="e">The event args.</param>
    protected virtual void OnClosed(EventArgs e)
    {
        Dispatcher.UIThread.Invoke(() => Closed?.Invoke(this, e));
    }

    protected virtual async Task ShowAnimation()
    {
        if (AnimateWindow)
        {

            var scaleTransform = new ScaleTransform(0.2, 0.2);
            RenderTransform = scaleTransform;
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(100),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter(ScaleTransform.ScaleXProperty, 1.0),
                            new Setter(ScaleTransform.ScaleYProperty, 1.0)
                        },
                        Cue = new Cue(1d)
                    }
                }
            };

            await animation.RunAsync(this);
            this.RenderTransform = null;
        }
    }

    protected virtual async Task ResizeAnimation(Rect oldPosition, Rect newPosition)
    {
        if (AnimateWindow)
        {
            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(100),
                FillMode = FillMode.Forward, // Ensure the animation holds the end value
                Children =
                {
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter(Canvas.LeftProperty, oldPosition.X),
                            new Setter(Canvas.TopProperty, oldPosition.Y),
                            new Setter(WidthProperty, oldPosition.Width),
                            new Setter(HeightProperty, oldPosition.Height)
                        },
                        Cue = new Cue(0d)
                    },
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter(Canvas.LeftProperty, newPosition.X),
                            new Setter(Canvas.TopProperty, newPosition.Y),
                            new Setter(WidthProperty, newPosition.Width),
                            new Setter(HeightProperty, newPosition.Height)
                        },
                        Cue = new Cue(1d)
                    }
                }
            };

            await animation.RunAsync(this);
        }
    }

    protected virtual async Task CloseAnimation()
    {
        if (AnimateWindow)
        {
            var scaleTransform = new ScaleTransform(1.0, 1.0);
            RenderTransform = scaleTransform;
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            var animation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(100),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter(ScaleTransform.ScaleXProperty, .2),
                            new Setter(ScaleTransform.ScaleYProperty, .2)
                        },
                        Cue = new Cue(1d)
                    }
                }
            };

            await animation.RunAsync(this);
            this.RenderTransform = null;
        }
    }


    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        this.GotFocus += (s, e) => Activate();
        this.LostFocus += (s, e) => Deactivate();

        //if (this.Theme == null)
        //    this.Theme = (ControlTheme)this.FindResource("ManagedWindow");
        _title = e.NameScope.Find<TextBlock>(PART_Title);

        _titleBar = e.NameScope.Find<Control>(PART_TitleBar);
        if (_titleBar != null)
        {
            SetupTitleBar(_titleBar);
        }

        var partMinimizeButton = e.NameScope.Find<Button>(PART_MinimizeButton);
        if (partMinimizeButton != null)
        {
            partMinimizeButton.Click += OnMinimizeClick;
        }

        var partMaximizeButton = e.NameScope.Find<Button>(PART_MaximizeButton);
        if (partMaximizeButton != null)
        {
            partMaximizeButton.Click += OnMaximizeClick;
        }
        var partRestoreButton = e.NameScope.Find<Button>(PART_RestoreButton);
        if (partRestoreButton != null)
        {
            partRestoreButton.Click += OnRestoreClick;
        }

        var partCloseButton = e.NameScope.Find<Button>(PART_CloseButton);
        if (partCloseButton != null)
        {
            partCloseButton.Click += OnCloseClick;
        }

        _content = e.NameScope.Find<ContentPresenter>(PART_ContentPresenter);

        _windowBorder = e.NameScope.Find<Border>(PART_WindowBorder);
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

            if (WindowState != WindowState.Maximized)
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
            if (WindowState != WindowState.Maximized)
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
            classes.Add(":dragging");
        else
            classes.Remove(":dragging");

        if (IsActive)
            classes.Add(":active");
        else
            classes.Remove(":active");

        if (ModalDialog != null)
            classes.Add(":hasdialog");
        else
            classes.Remove(":hasdialog");

        if (CanResize)
            classes.Remove(":noresize");
        else
            classes.Add(":noresize");

        classes.Remove(":minimized");
        classes.Remove(":normal");
        classes.Remove(":maximized");
        classes.Remove(":fullscreen");
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
            case WindowState.FullScreen:
                classes.Add(":fullscreen");
                Canvas.SetLeft(this, 0);
                Canvas.SetTop(this, 0);
                break;
        }

        classes.Remove(":noborder");
        if (SystemDecorations == SystemDecorations.None)
            classes.Add(":noborder");
    }

    public void BringToTop()
    {
        WindowManager.BringToTop(this);
    }

    void SetupResize(Border? border)
    {
        if (border == null)
            return;

        // when there is a box shadow we need to adjust the margin to allow it to be rendered.
        if (border.BoxShadow.Count > 0)
            border.Margin = new Thickness(0, 0, border.BoxShadow[0].OffsetX * 2, border.BoxShadow[0].OffsetY * 2);

        WindowEdge? edge = null;
        Point? start = null;
        border.PointerPressed += (i, e) =>
        {
            if (ModalDialog != null)
                return;

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
                    if (ModalDialog == null)
                    {
                        var edgeTemp = GetEdge(border, position);
                        border.Cursor = new Cursor(GetCursorForEdge(edgeTemp));
                    }
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
                       start.Value.X < left + border.BorderThickness.Left - border.Margin.Left;
        var rightEdge = start.Value.X >= right - border.BorderThickness.Right - border.Margin.Right &&
                        start.Value.X < right;
        var topEdge = start.Value.Y >= top &&
                        start.Value.Y < top + border.BorderThickness.Top - border.Margin.Top;
        var bottomEdge = start.Value.Y >= bottom - border.BorderThickness.Bottom - border.Margin.Bottom &&
                        start.Value.Y < bottom;
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
        if (CanResize)
        {
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            else if (WindowState == WindowState.Normal)
                WindowState = WindowState.Maximized;
            else if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
        }
    }


    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }


    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }


    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Maximized;
    }

    private void OnRestoreClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Normal;
    }

}
