using ATLab.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ATLab.Controls;

public partial class TestInterfaceCommunication : UserControl
{
    public static readonly StyledProperty<TestInterfaceCommunicationViewModel?> TestInterfaceCommunicationVmProperty =
        AvaloniaProperty.Register<TestInterfaceCommunication, TestInterfaceCommunicationViewModel?>(nameof(TestInterfaceCommunicationVm));

    public TestInterfaceCommunicationViewModel? TestInterfaceCommunicationVm
    {
        get => GetValue(TestInterfaceCommunicationVmProperty);
        set => SetValue(TestInterfaceCommunicationVmProperty, value);
    }
    
    public TestInterfaceCommunication()
    {
        InitializeComponent();
    }
}