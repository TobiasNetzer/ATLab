using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface ITestHardware
{
    bool[] StimChannelStates {get; set; }
    bool[] ExtStimChannelStates { get; set; }
    uint ActiveMeasChannelH { get; set; }
    uint ActiveMeasChannelL { get; set; }
    IHardwareInfo HardwareInfo { get; }
    Task<OperationResult> InitializeAsync();
    Task<OperationResult> SetStimChannels();
    Task<OperationResult> SetExtStimChannels();
}