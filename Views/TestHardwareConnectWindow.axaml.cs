using ATLab.ViewModels;
using Avalonia.Controls;

namespace ATLab.Views;

public partial class TestHardwareConnectWindow : Window
{
    public TestHardwareConnectWindow()
    {
        InitializeComponent();

        DataContextChanged += (sender, args) =>
        {
            if (DataContext is TestHardwareConnectWindowViewModel vm)
            {
                vm.RequestClose += () => this.Close();
            }
        };
    }
}