using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Iciclecreek.Avalonia.WindowManager
{
    /// <summary>
    /// This hosts a collection of windows 
    /// </summary>
    public partial class WindowsPanel : Canvas
    {
        private Panel _modalOverlay;

        public WindowsPanel()
        {
            this.ZIndex = 100000;
            _modalOverlay = new Panel()
            {
                IsVisible = false,
            };

            if (Application.Current.ApplicationLifetime.GetType().Name.Contains("Consolonia"))
            {
                _modalOverlay.Bind(Panel.BackgroundProperty, Resources.GetResourceObservable("ThemeShadeBrush"));
            }
            else
            {
                _modalOverlay.Bind(Panel.BackgroundProperty, Resources.GetResourceObservable("ManagedWindow_ModalBackgroundBrush"));
            }

            // Set initial size
            _modalOverlay.Width = this.Bounds.Width;
            _modalOverlay.Height = this.Bounds.Height;

            this.Children.Add(_modalOverlay);
        }

        private ManagedWindow? _modalDialog;

        public ManagedWindow? ModalDialog
        {
            get => _modalDialog;
            set
            {
                if (value != null && _modalDialog != null)
                    throw new NotSupportedException("Already showing a modal dialog for this window");

                _modalDialog = value;
                _modalOverlay.IsVisible = _modalDialog != null;
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == BoundsProperty)
            {
                var bounds = (Rect)change.NewValue!;
                _modalOverlay.Width = bounds.Width;
                _modalOverlay.Height = bounds.Height;
            }
        }

        /// <summary>
        /// Show a window as overlapping window in the current WindowsPanel
        /// </summary>
        /// <param name="window"></param>
        public void Show(ManagedWindow window)
        {
            window.Show(this);
        }

        /// <summary>
        /// Show a window as a modal dialog in the current Windows Panel.
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public Task ShowDialog(ManagedWindow window)
        {
            if (ModalDialog != null)
                throw new NotSupportedException("Already showing a dialog for this window");
            ModalDialog = window;
            return ModalDialog.ShowDialog(this);
        }
    }
}
