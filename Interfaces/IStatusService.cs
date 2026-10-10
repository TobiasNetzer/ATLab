using System;

namespace ATLab.Interfaces;

public interface IStatusService
{
    string Text { get; set; }
    double Progress { get; set; }
    bool IsIndeterminate { get; set; }
    bool IsVisible { get; set; }
    TimeSpan AnimationDuration { get;}
}