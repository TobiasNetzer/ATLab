using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ATLab.Controls;

/// <summary>Status text with progress bar.</summary>
public partial class StatusPanel : UserControl
{
    public static readonly StyledProperty<double> ProgressValueProperty =
        AvaloniaProperty.Register<StatusPanel, double>(nameof(ProgressValue));
    
    public static readonly StyledProperty<string> StatusTextProperty =
        AvaloniaProperty.Register<StatusPanel, string>(nameof(StatusText));
    
    public static readonly StyledProperty<bool> IsIndeterminateValueProperty =
        AvaloniaProperty.Register<StatusPanel, bool>(nameof(IsIndeterminateValue));
    
    public static readonly StyledProperty<TimeSpan> AnimationDurationValueProperty =
        AvaloniaProperty.Register<StatusPanel, TimeSpan>(nameof(AnimationDurationValue));
    
    public double ProgressValue
    {
        get => GetValue(ProgressValueProperty);
        set => SetValue(ProgressValueProperty, value);
    }
    
    public string StatusText
    {
        get => GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }
    
    public bool IsIndeterminateValue
    {
        get => GetValue(IsIndeterminateValueProperty);
        set => SetValue(IsIndeterminateValueProperty, value);
    }
    
    public TimeSpan AnimationDurationValue
    {
        get => GetValue(AnimationDurationValueProperty);
        set => SetValue(AnimationDurationValueProperty, value);
    }
    
    public StatusPanel()
    {
        InitializeComponent();
    }
}