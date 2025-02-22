using Avalonia.Controls;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iciclecreek.Avalonia.WindowManager;

namespace Demo
{
    internal static class Utils
    {
        public static void AdjustWindowSize(this ManagedWindow window, Rect rect)
        {
            var minWidth = (int)rect.Width / 4;
            var minHeight = (int)rect.Height / 4;
            var maxWidth = (int)rect.Width / 2;
            var maxHeight = (int)rect.Height / 2;

            if (window.WindowStartupLocation == WindowStartupLocation.Manual)
            {
                window.Position = new PixelPoint(Random.Shared.Next(0, (int)rect.Width - maxWidth),
                                              Random.Shared.Next(0, (int)rect.Height - maxHeight));
            }

            switch (window.SizeToContent)
            {
                case SizeToContent.Manual:
                    window.Width = Random.Shared.Next(minWidth, maxWidth);
                    window.Height = Random.Shared.Next(minHeight, maxHeight);
                    break;
                case SizeToContent.Width:
                    window.Height = Random.Shared.Next(minHeight, maxHeight);
                    break;
                case SizeToContent.Height:
                    window.Width = Random.Shared.Next(minWidth, maxWidth);
                    break;
                case SizeToContent.WidthAndHeight:
                    break;
            }
        }
    }
}
