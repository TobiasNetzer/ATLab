using System;
using ATLab.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Services;

public partial class StatusService : ObservableObject, IStatusService
{
    [ObservableProperty]
    private string _text = string.Empty;
    
    [ObservableProperty]
    private bool _isIndeterminate;

    [ObservableProperty]
    private bool _isVisible;

    private bool IsAnimationEnabled
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AnimationDuration));
        }
    } = true;

    public double Progress
    {
        get;
        set
        {
            if (Math.Abs(field - value) < 0.001)
                return;

            field = value;
            IsAnimationEnabled = field != 0;
            OnPropertyChanged();
        }
    }

    public TimeSpan AnimationDuration => IsAnimationEnabled ? TimeSpan.FromMilliseconds(200) : TimeSpan.Zero;
}