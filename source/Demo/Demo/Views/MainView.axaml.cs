using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Consolonia.Controls;
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
            
            window.ApplySettings(this.Bounds);

            window.Show(this);
        }

        private void OnClick(object? sender, RoutedEventArgs args)
        {
            MainViewModel model = (MainViewModel)DataContext;
            model.Counter++;
        }
    }
}