namespace ATLab.ViewModels;

/// <summary>Test Hardware tab: adapter information and diagnostics.</summary>
public class HardwareTabViewModel : ViewModelBase
{
    public TestHardwareInfoViewModel TestHardwareInfoViewModel { get; }
    public TestHardwareDiagnosticsViewModel TestHardwareDiagnosticsViewModel { get; }
    
    public HardwareTabViewModel(
        TestHardwareInfoViewModel testHardwareInfoViewModel,
        TestHardwareDiagnosticsViewModel testHardwareDiagnosticsViewModel)
    {
        TestHardwareInfoViewModel = testHardwareInfoViewModel;
        TestHardwareDiagnosticsViewModel = testHardwareDiagnosticsViewModel;
        
        SetLocalizedTitle("Tab_Hardware");
    }
}