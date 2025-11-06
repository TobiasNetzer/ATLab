using Avalonia.Controls;

namespace ATLab.Models;

public class AppSettings
{
    public string? LastComPort { get; set; } = "";
    public double WindowWidth { get; set; } = 800;
    public double WindowHeight { get; set; } = 600;
    public double WindowX { get; set; } = -1;
    public double WindowY { get; set; } = -1;
    public WindowState WindowState { get; set; } = WindowState.Normal;

}