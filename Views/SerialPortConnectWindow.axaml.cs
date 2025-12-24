using ATLab.ViewModels;
using Avalonia.Controls;

namespace ATLab.Views;

public partial class SerialPortConnectWindow : Window
{
    public SerialPortConnectWindow()
    {
        InitializeComponent();

        DataContextChanged += (sender, args) =>
        {
            if (DataContext is SerialPortConnectWindowViewModel vm)
            {
                vm.RequestClose += () => this.Close();
            }
        };
    }
}