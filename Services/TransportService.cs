using System.Text;
using System.Threading.Tasks;
using ATLab.Interfaces;

namespace ATLab.Services;

public class TransportService
{
    private readonly ICommunication _transport;
    private readonly Encoding _encoding = Encoding.ASCII;

    public TransportService(ICommunication transport)
    {
        _transport = transport;
    }

    public async Task<byte[]> QueryAsync(byte[] command, int timeoutMs = 1000)
    {
        return await _transport.SendAsync(command, timeoutMs);
    }

    public Task<byte[]> QueryAsync(string command, int timeoutMs = 1000)
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
