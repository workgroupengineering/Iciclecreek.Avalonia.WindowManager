using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Iciclecreek.Avalonia.WindowManager;
using System;

namespace Demo
{
    public partial class MainView : WindowManagerPanel
    {
        public MainView()
        {
            InitializeComponent();
        }

        private void OnAddWindowClick(object? sender, RoutedEventArgs args)
        {
            var window = new MyWindow()
            {
                WindowStartupLocation = Enum.Parse<WindowStartupLocation>((StartupLocationCombo.SelectedItem as ComboBoxItem).Tag.ToString()),
                SizeToContent = Enum.Parse<SizeToContent>(((ComboBoxItem)SizeToContentCombo.SelectedItem).Tag.ToString())
            };

            var minWidth = (int)Bounds.Width / 4;
            var minHeight = (int)Bounds.Height / 4;
            var maxWidth = (int)Bounds.Width / 2;
            var maxHeight = (int)Bounds.Height / 2;  

            if (window.WindowStartupLocation == WindowStartupLocation.Manual)
            {
                window.Position = new PixelPoint(Random.Shared.Next(0, (int)Bounds.Width - maxWidth),
                                              Random.Shared.Next(0, (int)Bounds.Height - maxHeight));
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

            ShowWindow(window);
        }
    }
}