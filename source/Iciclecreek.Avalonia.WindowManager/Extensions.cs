using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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
}
