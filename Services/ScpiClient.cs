using System.Text;
using System.Threading.Tasks;
using ATLab.Interfaces;

namespace ATLab.Services;

public class ScpiClient
{
    private readonly ISerialCommunication _transport;
    private readonly Encoding _encoding = Encoding.ASCII;

    public ScpiClient(ISerialCommunication transport)
    {
        _transport = transport;
    }

    public async Task<string> QueryAsync(string command, int timeoutMs = 1000)
    {
        var bytes = _encoding.GetBytes(command + "\n");
        var responseBytes = await _transport.SendAsync(bytes, timeoutMs);
        return _encoding.GetString(responseBytes).Trim();
    }

    public Task WriteAsync(string command)
    {
        var bytes = _encoding.GetBytes(command + "\n");
        _transport.SendRaw(bytes);
        return Task.CompletedTask;
    }
}
