using ATLab.ViewModels;
using Window = ShadUI.Window;

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

    public TestHardwareConnectWindow(TestHardwareConnectWindowViewModel vm) : this()
    {
        DataContext = vm;
    }
}