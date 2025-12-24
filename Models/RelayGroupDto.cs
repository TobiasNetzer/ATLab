using System.Collections.Generic;

namespace ATLab.Models;

public class RelayGroupDto
{
    public List<int> EnabledChannels { get; set; } = new();
    
    public RelayGroupDto() { }
    
    public RelayGroupDto(RelayGroupDto other)
    {
        EnabledChannels = new List<int>(other.EnabledChannels);
    }
}