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
            
            window.AdjustWindowSize(this.Bounds);

            ShowWindow(window);
        }
    }
}