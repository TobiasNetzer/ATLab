using ATLab.ViewModels;
using Avalonia.Controls;

namespace ATLab.Views;

public partial class SerialPortConnectWindow : Window
{
    public SerialPortConnectWindow()
    {
        InitializeComponent();
        DataContext = new SerialPortConnectWindowViewModel();

        if (DataContext is SerialPortConnectWindowViewModel vm)
        {
            vm.RequestClose += () => this.Close();
        }
    }
}