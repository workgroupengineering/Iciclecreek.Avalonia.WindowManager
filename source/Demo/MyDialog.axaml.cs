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

namespace Demo;

public partial class MyDialog : ManagedWindow
{
    private static int _dialogCount = 0;
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

    public MyDialog()
    {
        InitializeComponent();
        this.Background = brushes[Random.Shared.Next(0, brushes.Length)];

        this.DataContext = new MyDialogViewModel()
        {
            Title = $"New Dialog {++_dialogCount}"
        };
    }

    public MyDialogViewModel ViewModel => (MyDialogViewModel)DataContext;

    private void OnOK(object? sender, RoutedEventArgs args)
    {
        this.Close(ViewModel.Text);
    }

    private void OnCancel(object? sender, RoutedEventArgs args)
    {
        this.Close(null);
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

}

public partial class MyDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _text = "";

    [ObservableProperty]
    private string _title = "New Dialog";
}
