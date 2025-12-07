using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface ITestHardware
{
    bool[] StimChannelStates {get; set; }
    bool[] ExtStimChannelStates { get; set; }
    byte ActiveMeasChannelH { get; set; }
    byte ActiveMeasChannelL { get; set; }
    IHardwareInfo HardwareInfo { get; }
    Task<OperationResult> InitializeAsync();
    Task<OperationResult> UpdateRelayStates();
}