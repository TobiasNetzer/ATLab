using System.Collections.Generic;

namespace ATLab.Interfaces;

public interface IMessageFramer
{
    bool TryExtractMessages(List<byte> buffer, out List<byte[]> messages);
}