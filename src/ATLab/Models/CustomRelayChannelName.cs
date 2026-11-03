using System.ComponentModel;
using System.Text.Json.Serialization;
namespace ATLab.Models;

/// <summary>User defined name of a relay channel, shown in the relay selectors.</summary>
public class CustomRelayChannelName : INotifyPropertyChanged
{
    public int? ChannelIndex { get; init; }

    private string _channelName;
    private string? _netLabel;

    /// <summary>
    /// The label shown for the channel: the net name while the project uses net names as channel labels, otherwise the
    /// label entered by the user.
    /// </summary>
    public string ChannelName
    {
        get => _netLabel ?? _channelName;
        set
        {
            // A net label is not editable; a binding writing it back must not replace the user's label
            if (_netLabel is not null || _channelName == value)
                return;

            _channelName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChannelName)));
        }
    }

    /// <summary>The label entered by the user; this one is saved in the project.</summary>
    [JsonIgnore]
    public string ManualName => _channelName;

    /// <summary>Sets the label of the user, also while a net label is shown (e.g. when a project is loaded).</summary>
    public void SetManualName(string name)
    {
        if (_channelName == name)
            return;

        _channelName = name;
        if (_netLabel is null)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChannelName)));
    }

    /// <summary>Net name replacing the label, or null to show the user's label.</summary>
    [JsonIgnore]
    public string? NetLabel
    {
        get => _netLabel;
        set
        {
            if (_netLabel == value)
                return;

            var old = ChannelName;
            _netLabel = value;
            if (old != ChannelName)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChannelName)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CustomRelayChannelName(string channelName, int? channelIndex)
    {
        _channelName = channelName;
        ChannelIndex = channelIndex;
    }

    public override string ToString()
    {
        // this allows for keyboard selection
        return $"{ChannelIndex} {ChannelName}";
    }
}