using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;

namespace ATLab.Models;

public class CustomRelayChannelNames
{
    public List<string> StimChannelNames;
    public List<string> ExtStimChannelNames;
    public List<string> MeasChannelNames;

    public CustomRelayChannelNames()
    {
        StimChannelNames = new List<string>();
        
        ExtStimChannelNames = new List<string>();
        
        MeasChannelNames = new List<string>();
    }
}