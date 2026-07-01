using System.ComponentModel;
namespace ATLab.Models;

public class CustomRelayChannelName : INotifyPropertyChanged
{
    public int? ChannelIndex { get; init; }
    
    private string _channelName;
    public string ChannelName
    {
        get => _channelName;
        set
        {
            if (_channelName == value)
                return;
            
            _channelName = value;
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