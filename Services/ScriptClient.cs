using System.Text;
using System.Threading.Tasks;
using ATLab.Interfaces;

namespace ATLab.Services;

public class ScriptClient
{
    private readonly ICommunication _transport;
    private readonly Encoding _encoding = Encoding.ASCII;

    public ScriptClient(ICommunication transport)
    {
        _transport = transport;
    }

    public async Task<string> QueryAsync(byte[] command, int timeoutMs = 1000)
    {
        var responseBytes = await _transport.SendAsync(command, timeoutMs);
        return _encoding.GetString(responseBytes).Trim();
    }

    public Task<string> QueryAsync(string command, int timeoutMs = 1000)
    {
        var bytes = _encoding.GetBytes(command);
        return QueryAsync(bytes, timeoutMs);
    }

    public Task WriteAsync(byte[] command)
    {
        _transport.SendRaw(command);
        return Task.CompletedTask;
    }

    public Task WriteAsync(string command)
    {
        var bytes = _encoding.GetBytes(command);
        return WriteAsync(bytes);
    }
}
