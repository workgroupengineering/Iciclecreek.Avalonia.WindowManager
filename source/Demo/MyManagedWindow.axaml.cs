using Avalonia.Animation.Easings;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Iciclecreek.Avalonia.WindowManager;
using System;

namespace Demo;

public partial class MyManagedWindow : ManagedWindow
{
    private static int _windowCount = 0;
    private static IImmutableSolidColorBrush[] brushes =
        [
            Brushes.DarkBlue,
            Brushes.DarkGreen,
            Brushes.DarkCyan,
            Brushes.DarkGoldenrod,
            Brushes.DarkOrchid,
            Brushes.DarkOrange,
            Brushes.DarkGray,
            Brushes.DarkSalmon,
            Brushes.DarkSeaGreen,
            Brushes.DarkSlateGray,
            Brushes.DarkSlateBlue,
            Brushes.DarkTurquoise,
            Brushes.DarkViolet,
            Brushes.DarkKhaki,
            Brushes.DarkOliveGreen,
        ];

    public MyManagedWindow()
    {
        InitializeComponent();

        this.Background = brushes[_windowCount % brushes.Length];

        this.DataContext = new MyManagedWindowViewModel()
        {
            Counter = 0,
            Title = $"New Window {++_windowCount}"
        };
    }

    private void OnIncrement(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var vm = (MyManagedWindowViewModel?)this.DataContext;
        if (vm != null)
        {
            vm.Counter++;
        }
    }

    private void OnSpin(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
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

}

public partial class MyManagedWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private int _counter = 0;

    [ObservableProperty]
    private string _title = "New Window";
}
