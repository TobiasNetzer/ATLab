using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class TcpService : ICommunication
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private bool _disposed;
    private bool _connected;
    private readonly byte[] _terminationBytes;
    private readonly int _timeoutMs;

    public string Resource { get; }
    public bool IsConnected => _connected && _client != null && _client.Connected;

    public TcpService(string ipAddress, int port, int timeoutMs, TcpTerminationMode terminationMode)
    {
        Resource = $"{ipAddress}:{port}";
        _timeoutMs = timeoutMs;
        _terminationBytes = terminationMode switch
        {
            TcpTerminationMode.LF => new[] { (byte)'\n' },
            TcpTerminationMode.CR => new[] { (byte)'\r' },
            TcpTerminationMode.CRLF => new[] { (byte)'\r', (byte)'\n' },
            _ => Array.Empty<byte>()
        };
    }

    public async Task<OperationResult> ConnectAsync()
    {
        if (_disposed)
            return OperationResult.Failure("Instance already disposed.");

        if (IsConnected)
            return OperationResult.Success();

        try
        {
            var parts = Resource.Split(':');
            var ip = parts[0];
            var port = int.Parse(parts[1]);

            _client = new TcpClient();

            using var cts = new CancellationTokenSource(_timeoutMs);

            var connectTask = _client.ConnectAsync(ip, port, cts.Token);

            await connectTask;

            _stream = _client.GetStream();
            _stream.ReadTimeout = _timeoutMs;
            _stream.WriteTimeout = _timeoutMs;

            _connected = true;
            return OperationResult.Success();
        }
        catch (OperationCanceledException)
        {
            _client?.Dispose();
            _client = null;
            return OperationResult.Failure("Connection timed out.");
        }
        catch (Exception ex)
        {
            _client?.Dispose();
            _client = null;
            return OperationResult.Failure($"TCP connection error: {ex.Message}");
        }
    }

    public Task<OperationResult> DisconnectAsync()
    {
        if (_disposed)
            return Task.FromResult(OperationResult.Failure("Instance already disposed."));

        try
        {
            _stream?.Dispose();
            _stream = null;

            _client?.Dispose();
            _client = null;

            _connected = false;

            return Task.FromResult(OperationResult.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult.Failure($"Disconnect failed: {ex.Message}"));
        }
    }

    public async Task<OperationResult> ReconnectAsync()
    {
        await DisconnectAsync();
        return await ConnectAsync();
    }

    public void SendRaw(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (_disposed) throw new ObjectDisposedException(nameof(TcpService));
        if (!IsConnected || _stream == null) throw new InvalidOperationException("Not connected.");

        if (_terminationBytes.Length > 0)
        {
            var combined = new byte[data.Length + _terminationBytes.Length];
            Buffer.BlockCopy(data, 0, combined, 0, data.Length);
            Buffer.BlockCopy(_terminationBytes, 0, combined, data.Length, _terminationBytes.Length);
            _stream.Write(combined, 0, combined.Length);
        }
        else
        {
            _stream.Write(data, 0, data.Length);
        }
    }

    public async Task<byte[]> SendAsync(byte[] data, int timeoutMs = 1000)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (_disposed) throw new ObjectDisposedException(nameof(TcpService));
        if (!IsConnected || _stream == null) throw new InvalidOperationException("Not connected.");

        SendRaw(data);

        var buffer = new byte[8192];
        using var cts = new CancellationTokenSource(timeoutMs);
        try
        {
            var read = await _stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
            var result = new byte[read];
            Buffer.BlockCopy(buffer, 0, result, 0, read);
            return result;
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("Timed out waiting for response from device.");
        }
    }

    public async Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TcpService));
        if (!IsConnected || _stream == null) throw new InvalidOperationException("Not connected.");

        var buffer = new byte[8192];
        var read = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
        var result = new byte[read];
        Buffer.BlockCopy(buffer, 0, result, 0, read);
        return result;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _stream?.Dispose();
        _client?.Dispose();
        _connected = false;
    }
}