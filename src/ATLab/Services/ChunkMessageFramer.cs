using System.Collections.Generic;
using ATLab.Interfaces;

namespace ATLab.Services;

/// <summary>Takes whatever has been received so far as one message.</summary>
public class ChunkMessageFramer : IMessageFramer
{
    public bool TryExtractMessages(List<byte> buffer, out List<byte[]> messages)
    {
        messages = new List<byte[]>();

        if (buffer.Count == 0)
            return false;

        messages.Add(buffer.ToArray());
        buffer.Clear();
        return true;
    }
}
