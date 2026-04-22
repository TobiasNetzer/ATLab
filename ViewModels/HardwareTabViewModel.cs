namespace ATLab.ViewModels;

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
        
        Title = "Test Hardware";
    }
}