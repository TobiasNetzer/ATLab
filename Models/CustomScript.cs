using System;
using System.Collections.Generic;

namespace ATLab.Models;

public class CustomScript
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public List<CustomVariable> Variables { get; set; } = new();
    public List<ScriptCommand> Commands { get; set; } = new();
}