using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Iciclecreek.Avalonia.WindowManager;
using System;

namespace Demo
{
    public partial class MainWindow : Window
    {
        

        private int _counter = 3;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var width = Random.Shared.Next(100, 400);
            var height = Random.Shared.Next(100, 400);
            var position = new PixelPoint(Random.Shared.Next(0, (int)Bounds.Width - width),
                                          Random.Shared.Next(0, (int)Bounds.Height - height));
            var window = new MyManagedWindow()
            {
                Width = width,
                Height = height,
                Position = position,
            };

            WindowsPanel.ShowWindow(window);
        }
    }

}