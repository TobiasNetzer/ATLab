using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class TestHardwareSimulator : ITestHardware
{
    public IHardwareInfo HardwareInfo { get; }
    
    public bool[] StimChannelStates {get; set;}
    public bool[] ExtStimChannelStates { get; set; }
    public bool[] MeasChannelStates { get; set; }
    public byte ActiveMeasChannelH { get; set; }
    public byte ActiveMeasChannelL { get; set; }
    public byte UseExternalProbe { get; set; }

    public TestHardwareSimulator()
    {
        HardwareInfo = new DummyHardwareInfo();
        StimChannelStates = new  bool[HardwareInfo.StimChannelCount];
        ExtStimChannelStates = new  bool[HardwareInfo.ExtStimChannelCount];
        MeasChannelStates = new  bool[HardwareInfo.MeasChannelCount];
        
        ActiveMeasChannelH = 0;
        ActiveMeasChannelL = 0;
        UseExternalProbe = 0;
    }

    public async Task<OperationResult> InitializeAsync()
    {
      await Task.CompletedTask;
        return OperationResult.Success();
    }

    public async Task<OperationResult> UpdateRelayStates()
    {
        await Task.CompletedTask;
        return OperationResult.Success();
    }

    public async Task<OperationResult> ClearRelayStates()
    {
        await Task.CompletedTask;
        return OperationResult.Success();
    }

    public async Task<OperationResult<TestHardwareDiagnostics>> ExecuteSelfTest()
    {
        await Task.CompletedTask;
        return OperationResult<TestHardwareDiagnostics>.Success(new TestHardwareDiagnostics());
    }
}