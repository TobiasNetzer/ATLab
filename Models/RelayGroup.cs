using System.Collections.Generic;
using System.Linq;

namespace ATLab.Models;

public class RelayGroup
{
    public List<RelayChannelState> Channels { get; set; } = new();

    public RelayGroup(int channelCount)
    {
        for (int i = 0; i < channelCount; i++)
        {
            Channels.Add(new RelayChannelState
            {
                ChannelIndex = i + 1,   // 1-based for UI
                IsEnabled = false       // default state
            });
        }
    }
    
    public bool[] ToBoolArray()
    {
        return Channels.Select(c => c.IsEnabled).ToArray();
    }
}