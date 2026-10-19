using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ATLab.ViewModels;

namespace ATLab.Controls;

public partial class TestStepConfigurator : UserControl
{
    public TestStepConfigurator()
    {
        InitializeComponent();
    }

    public void FocusStepName()
    {
        if (!IsEffectivelyVisible || !IsEffectivelyEnabled)
            return;

        if (DataContext is TestStepConfiguratorViewModel vm)
            vm.IsExpanded = true;
        
        Dispatcher.UIThread.Post(() =>
        {
            if (StepNameTextBox.Focus())
                StepNameTextBox.SelectAll();
        }, DispatcherPriority.Loaded);
    }
}
