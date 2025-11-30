namespace ATLab.Models;

public class RelayChannel
{
    public bool IsEnabled { get; set; } = false;
    
    public string Name { get; set; }

    private RelayChannel(string channelName = "")
    {
        Name = channelName;
    }
}