using ATLab.CTIA;
using ATLab.Services;
using ATLab.ViewModels;
using ATLab.Wrappers;

namespace ATLab.DesignViewModels;

public class StimChannelDesignViewModel : StimChannelViewModel
{
    public StimChannelDesignViewModel() 
        : base(new CtiaHardware(new SimulationService()))
    {
        
        var stimGroup = new StimChannelGroup(new CtiaHardware(new SimulationService()));
        
        for (int i = 0; i < 16; i++)
        {
            StimChannels.Add(new RelayChannelViewModel(stimGroup, i, $"STIM CH"));
        }
    }
}