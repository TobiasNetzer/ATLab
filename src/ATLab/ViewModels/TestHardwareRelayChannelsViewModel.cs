using System.Collections.ObjectModel;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.ViewModels;

/// <summary>
/// Relay channel names of the project and the channel selectors for stimulus, external stimulus and measurement.
/// </summary>
public class TestHardwareRelayChannelsViewModel : ViewModelBase
{
    private readonly ProjectModel _projectModel;
    
    public ObservableCollection<CustomRelayChannelName> StimChannelNames =>
        _projectModel.StimChannelNames;

    public ObservableCollection<CustomRelayChannelName> ExtStimChannelNames =>
        _projectModel.ExtStimChannelNames;

    public ObservableCollection<CustomRelayChannelName> MeasChannelNames =>
        _projectModel.MeasChannelNames;

    /// <summary>
    /// Whether the nets of the channels are their labels; the labels of the user are then kept but not editable.
    /// </summary>
    public bool IsNetNamesAsChannelLabels
    {
        get => _projectModel.Board.IsNetNamesAsChannelLabels;
        set => _projectModel.Board.IsNetNamesAsChannelLabels = value;
    }

    public bool IsLabelEditingEnabled => !IsNetNamesAsChannelLabels;

    public StimChannelViewModel StimChannelViewModel { get; }
    public ExtStimChannelViewModel ExtStimChannelViewModel { get; }
    public MeasChannelViewModel MeasChannelViewModel { get; }

    public TestHardwareRelayChannelsViewModel(
        ProjectModel projectModel,
        ISettingsService settingsService)
    {
        _projectModel = projectModel;
        projectModel.Board.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(BoardLink.IsNetNamesAsChannelLabels))
            {
                OnPropertyChanged(nameof(IsNetNamesAsChannelLabels));
                OnPropertyChanged(nameof(IsLabelEditingEnabled));
            }
        };

        StimChannelViewModel = new StimChannelViewModel(StimChannelNames, settingsService);
        ExtStimChannelViewModel = new ExtStimChannelViewModel(ExtStimChannelNames, settingsService);
        MeasChannelViewModel = new MeasChannelViewModel(MeasChannelNames, settingsService);
    }
}