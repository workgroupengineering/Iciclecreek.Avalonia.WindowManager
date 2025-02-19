using Avalonia.Animation.Easings;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Iciclecreek.Avalonia.WindowManager;
using System;
using Avalonia;
using Avalonia.Interactivity;
using System.Linq;

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

    private void OnSpin(object? sender, RoutedEventArgs args)
    {
        // Create a rotate transform and apply it to the control

        var rotateAnimation = new Animation
        {
            Duration = TimeSpan.FromSeconds(1),
            Easing = new SineEaseInOut(),
            Children =
                    {
                        new KeyFrame
                        {
                            Cue = new Cue(0d),
                            Setters =
                            {
                                new Setter(RotateTransform.AngleProperty, 0d)
                            }
                        },
                        new KeyFrame
                        {
                            Cue = new Cue(1d),
                            Setters =
                            {
                                new Setter(RotateTransform.AngleProperty, 360d)
                            }
                        }
                    }
        };

        rotateAnimation.RunAsync(this);
    }

    private async void OnShowDialog(object? sender, RoutedEventArgs args)
    {
        var dialog = new MyDialog()
        {
            WindowStartupLocation = Enum.Parse<WindowStartupLocation>((StartupLocationCombo.SelectedItem as ComboBoxItem).Tag.ToString()),
            SizeToContent = Enum.Parse<SizeToContent>(((ComboBoxItem)SizeToContentCombo.SelectedItem).Tag.ToString()),
        };
        dialog.ViewModel.Text = ViewModel.Text;

        if (dialog.WindowStartupLocation == WindowStartupLocation.Manual)
        {
            dialog.Position = new PixelPoint(Random.Shared.Next(150, (int)Bounds.Width - 100),
                                          Random.Shared.Next(0, (int)Bounds.Height - 100));
        }

        switch (dialog.SizeToContent)
        {
            case SizeToContent.Manual:
                dialog.Width = Random.Shared.Next(300, 500);
                dialog.Height = Random.Shared.Next(300, 500);
                break;
            case SizeToContent.Width:
                dialog.Height = Random.Shared.Next(300, 500);
                break;
            case SizeToContent.Height:
                dialog.Width = Random.Shared.Next(300, 500);
                break;
            case SizeToContent.WidthAndHeight:
                break;
        }

        var result = await dialog.ShowDialog<string?>(this);
        if (result != null)
            ViewModel.Text = result;
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
