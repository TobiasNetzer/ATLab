using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLab.ViewModels;

public partial class BoundaryScanBusStepEditorViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ProjectModel _projectModel;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedBus), nameof(IsI2C), nameof(I2CAddressText), nameof(BytesToReadText))]
    private TestStep? _testStep;

    /// <summary>Buses are defined in the Boundary Scan tab.</summary>
    public ObservableCollection<BoundaryScanBus> Buses => _projectModel.BoundaryScan.Buses;

    public BoundaryScanBus? SelectedBus
    {
        get => Buses.FirstOrDefault(b => b.Id == TestStep?.BoundaryScanBus.BusId);
        set
        {
            // The ComboBox clears its selection while another step is loaded, that must not unassign the bus
            if (value == null || TestStep == null)
                return;

            TestStep.BoundaryScanBus.BusId = value.Id;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsI2C));
        }
    }

    public bool IsI2C => SelectedBus?.Type == BoundaryScanBusType.I2C;

    /// <summary>7 bit address, shown decimal like the Interface editor. Hex with 0x prefix is accepted too.</summary>
    public string I2CAddressText
    {
        get => (TestStep?.BoundaryScanBus.I2CAddress ?? 0).ToString(CultureInfo.InvariantCulture);
        set
        {
            if (TestStep == null)
                return;

            var text = value.Trim();
            var parsed = text.StartsWith("0x", System.StringComparison.OrdinalIgnoreCase)
                ? int.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex) ? hex : -1
                : int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dec) ? dec : -1;

            if (parsed is >= 0 and <= 0x7F)
                TestStep.BoundaryScanBus.I2CAddress = parsed;

            OnPropertyChanged();
        }
    }

    /// <summary>Text box like in the Interface editor, invalid input counts as 0.</summary>
    public string BytesToReadText
    {
        get => (TestStep?.BoundaryScanBus.BytesToRead ?? 0).ToString(CultureInfo.InvariantCulture);
        set
        {
            if (TestStep == null)
                return;

            TestStep.BoundaryScanBus.BytesToRead =
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) && count > 0 ? count : 0;
        }
    }

    public BoundaryScanBusStepEditorViewModel(ISettingsService settingsService, ProjectModel projectModel)
    {
        _settingsService = settingsService;
        _projectModel = projectModel;

        IsExpanded = settingsService.Settings.IsBoundaryScanBusEditorExpanded;
    }

    partial void OnIsExpandedChanged(bool value)
    {
        _settingsService.Settings.IsBoundaryScanBusEditorExpanded = value;
    }

    public void LoadTestStep(TestStep testStep)
    {
        TestStep = testStep;
    }

    [RelayCommand]
    private void ToggleExpanded() => IsExpanded = !IsExpanded;
}