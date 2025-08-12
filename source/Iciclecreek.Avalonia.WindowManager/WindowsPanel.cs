using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using System;
using System.Threading.Tasks;

namespace Iciclecreek.Avalonia.WindowManager
{
    /// <summary>
    /// This hosts a collection of windows 
    /// </summary>
    [TemplatePart(PART_Windows, typeof(Canvas))]
    [TemplatePart(PART_ModalOverlay, typeof(Panel))]
    public partial class WindowsPanel : ContentControl
    {
        public const string PART_Windows = "PART_Windows";
        public const string PART_ModalOverlay = "PART_ModalOverlay";

        private Canvas? _canvas;
        private Panel? _modalOverlay;
        private ManagedWindow? _modalDialog;

        public WindowsPanel()
        {
        }


        public ManagedWindow? ModalDialog
        {
            get => _modalDialog;
            set
            {
                if (value != null && _modalDialog != null)
                    throw new NotSupportedException("Already showing a modal dialog for this window");

                _modalDialog = value;
                if (_modalDialog != null && _modalOverlay != null)
                {
                    _modalOverlay.ZIndex = _modalDialog.ZIndex++;
                    _modalOverlay.IsVisible = true;

                    _modalDialog.Closed += (s, e) =>
                    {
                        _modalDialog = null;
                        if (_modalOverlay != null)
                            _modalOverlay.IsVisible = false;
                    };
                }
            }
        }

        public Controls Windows => _canvas.Children;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _canvas = e.NameScope.Find<Canvas>(PART_Windows) ?? throw new ArgumentNullException(PART_Windows);
            _modalOverlay = e.NameScope.Find<Panel>(PART_ModalOverlay) ?? throw new ArgumentNullException(PART_ModalOverlay);
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
