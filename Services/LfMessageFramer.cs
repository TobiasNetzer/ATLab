using System.Collections.Generic;
using ATLab.Interfaces;

namespace ATLab.Services;

public class LfMessageFramer : IMessageFramer
{
    public bool TryExtractMessages(List<byte> buffer, out List<byte[]> messages)
    {
        messages = new List<byte[]>();
        int idx;

        while ((idx = buffer.IndexOf((byte)'\n')) >= 0)
        {
            var msg = buffer.GetRange(0, idx + 1).ToArray();
            buffer.RemoveRange(0, idx + 1);
            messages.Add(msg);
        }

        return messages.Count > 0;
    }
}