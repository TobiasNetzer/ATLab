using System;

namespace ATLab.Interfaces;

/// <summary>
/// Progress and status text shown in the status panel during long actions, e.g. long delays or external programs.
/// </summary>
public interface IStatusService
{
    string Text { get; set; }
    double Progress { get; set; }
    bool IsIndeterminate { get; set; }
    bool IsVisible { get; set; }
    TimeSpan AnimationDuration { get;}
}