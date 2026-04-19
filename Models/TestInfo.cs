using System;

namespace ATLab.Models;

public class TestInfo
{
    public string? ProjectName { get; init; }
    public string? Operator { get; init; }
    
    public string Date { get; } = DateTime.Now.ToString("dd.MM.yyyy");
    public string Time { get; } = DateTime.Now.ToString("HH:mm:ss");
    public string? Duration { get; init; }
    
    public string? SerialNumber { get; init; }
    public DeviceUnderTestInfo DeviceUnderTestInfo { get; init; } = new();
}