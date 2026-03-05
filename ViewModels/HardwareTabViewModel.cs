namespace ATLab.ViewModels;

public class HardwareTabViewModel : ViewModelBase
{
    public TestHardwareInfoViewModel TestHardwareInfoViewModel { get; }
    public HardwareTabViewModel(TestHardwareInfoViewModel testHardwareInfoViewModel)
    {
        TestHardwareInfoViewModel = testHardwareInfoViewModel;
        
        Title = "Hardware";
    }
}