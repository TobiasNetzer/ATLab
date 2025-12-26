using System;
using System.Collections.Generic;

namespace ATLab.Models;

public class ScpiScript
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public List<ScpiVariable> Variables { get; set; } = new();
    public List<ScpiCommand> Commands { get; set; } = new();
}
