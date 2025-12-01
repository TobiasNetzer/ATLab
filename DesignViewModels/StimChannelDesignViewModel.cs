using ATLab.CTIA;
using ATLab.Services;
using ATLab.ViewModels;

namespace ATLab.DesignViewModels;

public class StimChannelDesignViewModel : StimChannelViewModel
{
    public StimChannelDesignViewModel() 
        : base(new CtiaHardware(new SimulationService()))
    {
        for (int i = 0; i < 16; i++)
        {
            StimChannels.Add(new RelayChannelViewModel(new CtiaHardware(new SimulationService()), i, $"STIM CH {i + 1}"));
        }
    }
}