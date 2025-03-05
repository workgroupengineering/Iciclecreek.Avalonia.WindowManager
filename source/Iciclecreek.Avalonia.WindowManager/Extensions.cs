using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using System;
using System.Linq;
using System.Reflection;

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
            WindowManagerPanel windowManager = visual as WindowManagerPanel ??
                visual.FindAncestorOfType<WindowManagerPanel>() ??
                TopLevel.GetTopLevel(visual).FindDescendantOfType<WindowManagerPanel>();

            ArgumentNullException.ThrowIfNull(windowManager, nameof(WindowManagerPanel));
            windowManager.AddWindow(window);
        }
    }
}
