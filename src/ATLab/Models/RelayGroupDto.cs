using System.Collections.Generic;

namespace ATLab.Models;

/// <summary>Stored form of a relay group: only the numbers of the enabled channels.</summary>
public class RelayGroupDto
{
    public List<int> EnabledChannels { get; set; } = new();
    
    public RelayGroupDto() { }
    
    public RelayGroupDto(RelayGroupDto other)
    {
        EnabledChannels = new List<int>(other.EnabledChannels);
    }
}