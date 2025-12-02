namespace ATLab.Interfaces;

public interface IChannelGroup
{
    bool this[int index] { get; set; }
    void CommitChanges();
}
