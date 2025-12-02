using ATLab.CTIA;
using ATLab.Services;
using ATLab.ViewModels;
using ATLab.Wrappers;

namespace ATLab.DesignViewModels;

public class ExtStimChannelDesignViewModel : ExtStimChannelViewModel
{
    public ExtStimChannelDesignViewModel() 
        : base(new CtiaHardware(new SimulationService()))
    {
        
        var extStimGroup = new ExtStimChannelGroup(new CtiaHardware(new SimulationService()));
        
        for (int i = 0; i < 4; i++)
        {
            ExtStimChannels.Add(new RelayChannelViewModel(extStimGroup, i, $"EXT STIM CH"));
        }
    }
}