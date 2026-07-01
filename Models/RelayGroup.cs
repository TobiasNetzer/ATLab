using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace ATLab.Models;

public class RelayGroup : ObservableObject
{
    public List<RelayChannelState> Channels { get; set; } = new();

    public RelayGroup(int channelCount)
    {
        for (int i = 0; i < channelCount; i++)
        {
            var channel = new RelayChannelState
            {
                ChannelIndex = i + 1,   // 1-based for UI
                IsEnabled = false       // default state
            };
            channel.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Channels));
            Channels.Add(channel);
        }
    }
    
    public bool[] ToBoolArray()
    {
        return Channels.Select(c => c.IsEnabled).ToArray();
    }
    
    public RelayGroupDto ToDto()
    {
        return new RelayGroupDto
        {
            EnabledChannels = Channels
                .Where(c => c.IsEnabled)
                .Select(c => c.ChannelIndex)
                .ToList()
        };
    }

    public void ApplyDto(RelayGroupDto dto)
    {
        foreach (var channel in Channels)
            channel.IsEnabled = dto.EnabledChannels.Contains(channel.ChannelIndex);
    }
}