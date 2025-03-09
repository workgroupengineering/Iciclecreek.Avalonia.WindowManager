using Avalonia.Animation.Easings;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Iciclecreek.Avalonia.WindowManager;
using System;
using Avalonia.Interactivity;

namespace Demo;

public partial class MyWindow : ManagedWindow
{
    private static int _windowCount = 0;
    private static IImmutableSolidColorBrush[] brushes =
        [
            Brushes.LightBlue,
            Brushes.LightGreen,
            Brushes.LightCyan,
            Brushes.LightSalmon,
            Brushes.LightSeaGreen,
            Brushes.LightSlateGray,
            Brushes.LightCoral,
            Brushes.LightGoldenrodYellow,
            Brushes.LightPink
        ];

    public MyWindow()
    {
        InitializeComponent();

        this.Background = brushes[Random.Shared.Next(0, brushes.Length)];

        this.DataContext = new MyWindowViewModel()
        {
            Counter = 0,
            Title = $"New Window {++_windowCount}"
        };
    }

    public MyWindowViewModel ViewModel => (MyWindowViewModel)DataContext;

    private void OnIncrement(object? sender, RoutedEventArgs args)
    {
        var vm = (MyWindowViewModel?)this.DataContext;
        if (vm != null)
        {
            vm.Counter++;
        }
    }

    private void OnColor(object? sender, RoutedEventArgs args)
    {
        this.Background = brushes[Random.Shared.Next(0, brushes.Length)];
    }


    private async void OnShowDialog(object? sender, RoutedEventArgs args)
    {
        var dialog = new MyDialog()
        {
            WindowStartupLocation = Enum.Parse<WindowStartupLocation>((StartupLocationCombo.SelectedItem as ComboBoxItem).Tag.ToString()),
            SizeToContent = Enum.Parse<SizeToContent>(((ComboBoxItem)SizeToContentCombo.SelectedItem).Tag.ToString()),
        };
        dialog.ViewModel.Text = ViewModel.Text;

        dialog.SizeToBounds(this.Bounds);

        var result = await dialog.ShowDialog<string?>(this);
        if (result != null)
            ViewModel.Text = result;
    }

    private void OnSystemDecoration_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        if (this.SystemDecorationCombo?.SelectedItem is ComboBoxItem item)
        {
            var decoration = Enum.Parse<SystemDecorations>(item.Tag.ToString());
            this.SystemDecorations = decoration;
        }
    }
}

public partial class MyWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private int _counter = 0;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private string _title = "New Window";
}
