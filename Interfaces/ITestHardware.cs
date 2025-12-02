using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface ITestHardware
{
    bool[] StimChannelStates {get; set; }
    bool[] ExtStimChannelStates { get; set; }
    bool[] MeasChannelStatesH { get; set; }
    bool[] MeasChannelStatesL { get; set; }
    IHardwareInfo HardwareInfo { get; }
    Task<OperationResult> InitializeAsync();
    Task<OperationResult> SetStimChannels();
    Task<OperationResult> SetExtStimChannels();
}