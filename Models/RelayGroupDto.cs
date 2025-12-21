using System.Collections.Generic;

namespace ATLab.Models;

public class RelayGroupDto
{
    public List<int> EnabledChannels { get; set; } = new();
}
