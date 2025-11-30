using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface IUniversalTestHardwareInterface
{
    bool[] StimChannelStates {get; set; }
    bool[] ExtStimChannelStates { get; set; }
    bool[] MeasChannelStatesH { get; set; }
    bool[] MeasChannelStatesL { get; set; }
    IHardwareInfoProvider HardwareInfoProvider { get; }
    Task<OperationResult> InitializeAsync();
    Task<OperationResult> SetStimChannels();
}