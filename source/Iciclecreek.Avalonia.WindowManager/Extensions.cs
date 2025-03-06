using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Iciclecreek.Avalonia.WindowManager
{
    internal static class ActivatorEx
    {
        public static T CreateInstance<T>(params object[] args)
        {
            var types = args?.Select(arg => arg.GetType()).ToArray();
            // Get the internal constructor with the specified parameter types
            ConstructorInfo constructor = typeof(T).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                types!,
                null
            )!;

            // Create an instance of the class using the constructor
            var result = constructor.Invoke(args);
            ArgumentNullException.ThrowIfNull(result);
            return (T)result;
        }
    }

    public static class PublicUtilities
    {
        public static void ShowWindow(this Visual visual, ManagedWindow window)
        {
            WindowsPanel? windowManager = visual as WindowsPanel ??
                visual.FindAncestorOfType<WindowsPanel>() ??
                TopLevel.GetTopLevel(visual).FindDescendantOfType<WindowsPanel>();

            ArgumentNullException.ThrowIfNull(windowManager, nameof(WindowsPanel));
            windowManager.AddWindow(window);
        }

        public static void ShowDialog(this Visual visual, ManagedWindow window)
        {
            WindowsPanel? windowsPanel = visual as WindowsPanel ??
                visual.FindAncestorOfType<WindowsPanel>() ??
                TopLevel.GetTopLevel(visual).FindDescendantOfType<WindowsPanel>();

            ArgumentNullException.ThrowIfNull(windowsPanel, nameof(WindowsPanel));

            ManagedWindow owner = visual.FindAncestorOfType<ManagedWindow>()
                ?? CreateOverlayWindow(windowsPanel, window);

            window.ShowDialog(owner);
        }

        private static ManagedWindow CreateOverlayWindow(WindowsPanel wm, ManagedWindow childWindow)
        {
            var overlay = new ManagedWindow()
            {
                Position = new PixelPoint((ushort)wm.Bounds.Left, (ushort)wm.Bounds.Top),
                SystemDecorations = SystemDecorations.None,
                Background = Brushes.Transparent,
                ShowActivated = false,
                Width = wm.Bounds.Width,
                Height = wm.Bounds.Height,
                AnimateWindow = false
            };
            EventHandler<SizeChangedEventArgs> OnResized = (_, _) =>
            {
                overlay.Width = wm.Bounds.Width;
                overlay.Height = wm.Bounds.Height;
            };
            wm.SizeChanged += OnResized;

            childWindow.Closed += (_, _) =>
            {
                wm.SizeChanged -= OnResized;
                overlay.Close();
            };

            wm.AddWindow(overlay);
            return overlay;
        }
    }
}
