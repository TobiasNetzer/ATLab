using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class TestHardwareDiagnosticsViewModel : ViewModelBase
{
    private readonly ITestHardware _testHardware;
    
    [ObservableProperty]
    private bool _isAcknowledged;
    
    [ObservableProperty]
    private bool _isSelfTestRunning;
    
    [ObservableProperty]
    private string _selfTestResult = string.Empty;
    
    public ObservableCollection<string> DefectiveRelaysMatrixH { get; } = new();
    public ObservableCollection<string> DefectiveRelaysMatrixL { get; } = new();
    
    [ObservableProperty]
    private bool _hasDefectiveRelays;

    [ObservableProperty]
    private bool _selfTestPassed;
    
    public TestHardwareDiagnosticsViewModel(ITestHardware testHardware)
    {
        _testHardware = testHardware;
    }

    partial void OnIsAcknowledgedChanged(bool value)
    {
        RunSelfTestCommand.NotifyCanExecuteChanged();
    }

    private bool WarningAcknowledged => IsAcknowledged;
    
    [RelayCommand(CanExecute = nameof(WarningAcknowledged))]
    private async Task RunSelfTest()
    {
        IsSelfTestRunning = true;
        HasDefectiveRelays = false;
        DefectiveRelaysMatrixH.Clear();
        DefectiveRelaysMatrixL.Clear();
        SelfTestResult = string.Empty;

        try
        {
            var result = await _testHardware.ExecuteSelfTest();

            if (!result.IsSuccess)
            {
                SelfTestPassed = false;
                SelfTestResult = $"Relay Matrix Test Failed: {result.ErrorMessage}";
                return;
            }

            var defectiveRelays = result.Value;
            
            if (defectiveRelays == null)
                return;

            if (defectiveRelays.DefectiveRelaysMatrixH.Count == 0 && defectiveRelays.DefectiveRelaysMatrixL.Count == 0)
            {
                SelfTestPassed = true;
                SelfTestResult = "Relay Matrix Test Passed";
            }
            else
            {
                HasDefectiveRelays = true;
                SelfTestPassed = false;
                SelfTestResult = "Defective Relays Detected";

                foreach (var relay in defectiveRelays.DefectiveRelaysMatrixH)
                    DefectiveRelaysMatrixH.Add(relay);
                
                foreach (var relay in defectiveRelays.DefectiveRelaysMatrixL)
                    DefectiveRelaysMatrixL.Add(relay);
            }
        }
        finally
        {
            IsSelfTestRunning = false;
        }
    }
}