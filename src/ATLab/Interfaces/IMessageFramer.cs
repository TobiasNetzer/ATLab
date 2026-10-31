using System.Collections.Generic;

namespace ATLab.Interfaces;

/// <summary>Splits received bytes into complete messages, e.g. by line terminator or by timeout.</summary>
public interface IMessageFramer
{
    bool TryExtractMessages(List<byte> buffer, out List<byte[]> messages);
}