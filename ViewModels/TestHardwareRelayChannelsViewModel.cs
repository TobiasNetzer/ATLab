using System.Collections.ObjectModel;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.ViewModels;

public class TestHardwareRelayChannelsViewModel : ViewModelBase
{
    private readonly ProjectModel _projectModel;
    
    public ObservableCollection<CustomRelayChannelName> StimChannelNames =>
        _projectModel.StimChannelNames;

    public ObservableCollection<CustomRelayChannelName> ExtStimChannelNames =>
        _projectModel.ExtStimChannelNames;

    public ObservableCollection<CustomRelayChannelName> MeasChannelNames =>
        _projectModel.MeasChannelNames;

    public StimChannelViewModel StimChannelViewModel { get; }
    public ExtStimChannelViewModel ExtStimChannelViewModel { get; }
    public MeasChannelViewModel MeasChannelViewModel { get; }

    public TestHardwareRelayChannelsViewModel(
        ProjectModel projectModel,
        ISettingsService settingsService)
    {
        _projectModel = projectModel;

        StimChannelViewModel = new StimChannelViewModel(StimChannelNames, settingsService);
        ExtStimChannelViewModel = new ExtStimChannelViewModel(ExtStimChannelNames, settingsService);
        MeasChannelViewModel = new MeasChannelViewModel(MeasChannelNames, settingsService);
    }
}