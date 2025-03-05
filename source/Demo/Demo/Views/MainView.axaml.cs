using Avalonia.Controls;
using Avalonia.Interactivity;
using Demo.ViewModels;
using System;

namespace Demo.Views
{
    public partial class MainView : Grid
    {
        public MainView()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();
        }

        private void OnAddWindowClick(object? sender, RoutedEventArgs args)
        {
            var window = new MyWindow()
            {
                WindowStartupLocation = Enum.Parse<WindowStartupLocation>((StartupLocationCombo.SelectedItem as ComboBoxItem).Tag.ToString()),
                SizeToContent = Enum.Parse<SizeToContent>(((ComboBoxItem)SizeToContentCombo.SelectedItem).Tag.ToString())
            };
            
            window.AdjustWindowSize(this.Bounds);

            WindowManager.AddWindow(window);
        }

        private void OnClick(object? sender, RoutedEventArgs args)
        {
            MainViewModel model = (MainViewModel)DataContext;
            model.Counter++;
        }
    }
}