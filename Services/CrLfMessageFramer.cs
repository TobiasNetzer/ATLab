using System.Collections.Generic;
using ATLab.Interfaces;

namespace ATLab.Services;

public class CrLfMessageFramer : IMessageFramer
{
    public bool TryExtractMessages(List<byte> buffer, out List<byte[]> messages)
    {
        messages = new List<byte[]>();

        int idx;
        while ((idx = buffer.IndexOf((byte)'\n')) >= 0)
        {
            int end = idx + 1;

            var msg = buffer.GetRange(0, end).ToArray();
            buffer.RemoveRange(0, end);

            messages.Add(msg);
        }

        return messages.Count > 0;
    }
}
