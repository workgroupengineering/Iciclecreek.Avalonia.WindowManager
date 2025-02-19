using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace Demo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
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

            if (window.WindowStartupLocation == WindowStartupLocation.Manual)
            {
                window.Position = new PixelPoint(Random.Shared.Next(150, (int)Bounds.Width - 100),
                                              Random.Shared.Next(0, (int)Bounds.Height - 100));
            }

            switch (window.SizeToContent)
            {
                case SizeToContent.Manual:
                    window.Width = Random.Shared.Next(300, 500);
                    window.Height = Random.Shared.Next(300, 500);
                    break;
                case SizeToContent.Width:
                    window.Height = Random.Shared.Next(300, 500);
                    break;
                case SizeToContent.Height:
                    window.Width = Random.Shared.Next(300, 500);
                    break;
                case SizeToContent.WidthAndHeight:
                    break;
            }

            WindowManager.ShowWindow(window);
        }
    }
}