using CommunityToolkit.Mvvm.ComponentModel;

namespace Demo.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _greeting = "Welcome to Avalonia!";

        [ObservableProperty]
        private int _counter;

        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        private string _title = "New Window";

    }
}
