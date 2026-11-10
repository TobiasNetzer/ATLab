using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.FixtureDesigner.Fixture;
using ATLab.FixtureDesigner.Localization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.FixtureDesigner.ViewModels;

/// <summary>A color of the settings: probes, a kind of pin or rod, or a kind of plate.</summary>
public partial class ColorOption : ObservableObject
{
    private readonly Action<ColorOption> _changed;
    private bool _isSetting;

    public ColorOption(ColorItem item, Action<ColorOption> changed)
    {
        Item = item;
        _changed = changed;
        _color = Color.Parse(FixtureColors.Get(item));
    }

    public ColorItem Item { get; }

    public string Label => Localizer.Get("Color_" + Item);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Brush))]
    private Color _color;

    public IBrush Brush => new SolidColorBrush(Color);

    partial void OnColorChanged(Color value)
    {
        if (!_isSetting)
            _changed(this);
    }

    /// <summary>A color that is not a choice of the user, e.g. the default.</summary>
    public void SetColor(Color color)
    {
        _isSetting = true;
        Color = color;
        _isSetting = false;
    }

    public void RefreshText() => OnPropertyChanged(nameof(Label));
}

/// <summary>
/// The user's colors of probes, pins, rods and plate outlines: in the board view and in the build sheet drawings, kept in
/// the Designer's settings.
/// </summary>
public partial class MainWindowViewModel
{
    public IReadOnlyList<ColorOption> ColorOptions { get; private set; } = [];

    public IBrush ProbeBrush => BrushOf(ColorItem.Probe);
    public IBrush ToolingPinBrush => BrushOf(ColorItem.ToolingPin);
    public IBrush PreCenteringPinBrush => BrushOf(ColorItem.PreCenteringPin);
    public IBrush SupportPinBrush => BrushOf(ColorItem.SupportPin);
    public IBrush PushRodBrush => BrushOf(ColorItem.PushRod);

    /// <summary>The pockets of the outline being edited have the color of the plate it belongs to; those of the shared one that of the probe plates.</summary>
    public IBrush OverlayBrush => BrushOf(SelectedOutlineTarget?.Plate is { } plate ? FixtureColors.Of(plate) : ColorItem.ProbePlate);

    private static IBrush BrushOf(ColorItem item) => new SolidColorBrush(Color.Parse(FixtureColors.Get(item)));

    private void InitializeColors()
    {
        FixtureColors.Load(_settings?.MarkerColors);
        ColorOptions = Enum.GetValues<ColorItem>().Select(i => new ColorOption(i, OnColorChanged)).ToList();
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedOutlineTarget))
                OnPropertyChanged(nameof(OverlayBrush));
        };
    }

    private void OnColorChanged(ColorOption option)
    {
        FixtureColors.Set(option.Item, ATLab.Pcb.Controls.LayerColors.ToHex(option.Color));
        AfterColorsChanged();
    }

    [RelayCommand]
    private void ResetColors()
    {
        foreach (var option in ColorOptions)
        {
            FixtureColors.Set(option.Item, FixtureColors.Default(option.Item));
            option.SetColor(Color.Parse(FixtureColors.Default(option.Item)));
        }

        AfterColorsChanged();
    }

    private void AfterColorsChanged()
    {
        foreach (var name in new[] { nameof(ProbeBrush), nameof(ToolingPinBrush), nameof(PreCenteringPinBrush), nameof(SupportPinBrush),
                     nameof(PushRodBrush), nameof(OverlayBrush) })
            OnPropertyChanged(name);

        UpdateOutlineState();

        if (_settings is null)
            return;

        _settings.MarkerColors = FixtureColors.Changed();

        // The picker changes the color continuously while dragging; the file is written once it rests
        _saveColorsTimer ??= new Avalonia.Threading.DispatcherTimer(TimeSpan.FromMilliseconds(600), Avalonia.Threading.DispatcherPriority.Background,
            (_, _) =>
            {
                _saveColorsTimer!.Stop();
                _settings.Save();
            });
        _saveColorsTimer.Stop();
        _saveColorsTimer.Start();
    }

    private Avalonia.Threading.DispatcherTimer? _saveColorsTimer;
}
