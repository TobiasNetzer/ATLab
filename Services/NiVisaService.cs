using System;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using Ivi.Visa;
using NationalInstruments.Visa;

namespace ATLab.Services;

public class NiVisaService : ICommunication, IDisposable
{
    private MessageBasedSession? _session;
    private bool _disposed;
    private bool _connected;

    private readonly int _timeoutMs;
    private readonly byte _terminationChar;

    public string Resource { get; }
    public bool IsConnected => _connected;

    public NiVisaService(string resource, int timeoutMs, byte terminationChar)
    {
        Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        _timeoutMs = timeoutMs;
        _terminationChar = terminationChar;
    }

    public async Task<OperationResult> ConnectAsync()
    {
        if (_disposed)
            return OperationResult.Failure("Instance already disposed.");

        if (_connected)
            return OperationResult.Success();

        try
        {
            await Task.Run(() =>
            {
                var rm = new ResourceManager();
                _session = (MessageBasedSession)rm.Open(Resource);
                _session.TimeoutMilliseconds = _timeoutMs;
                _session.TerminationCharacterEnabled = true;
                _session.TerminationCharacter = _terminationChar;

                _connected = true;
            });

            return OperationResult.Success();
        }
        catch (VisaException ex)
        {
            _connected = false;
            _session?.Dispose();
            _session = null;
            return OperationResult.Failure($"VISA error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _connected = false;
            _session?.Dispose();
            _session = null;
            return OperationResult.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<OperationResult> ReconnectAsync()
    {
        try
        {
            await DisconnectAsync();
            return await ConnectAsync();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Reconnect failed: {ex.Message}");
        }
    }

    public async Task<OperationResult> DisconnectAsync()
    {
        if (_disposed)
            return OperationResult.Failure("Instance already disposed.");

        try
        {
            await Task.Run(() =>
            {
                _session?.Dispose();
                _session = null;
                _connected = false;
            });

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Disconnect failed: {ex.Message}");
        }
    }

    public void SendRaw(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (_disposed) throw new ObjectDisposedException(nameof(NiVisaService));
        if (_session == null) throw new InvalidOperationException("Not connected.");

        _session.RawIO.Write(data);
    }

    public async Task<byte[]> SendAsync(byte[] data, int timeoutMs = 1000)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (_disposed) throw new ObjectDisposedException(nameof(NiVisaService));
        if (_session == null) throw new InvalidOperationException("Not connected.");

        _session.TimeoutMilliseconds = timeoutMs;

        return await Task.Run(() =>
        {
            _session.RawIO.Write(data);
            return _session.RawIO.Read();
        });
    }

    public async Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(NiVisaService));
        if (_session == null) throw new InvalidOperationException("Not connected.");

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _session.RawIO.Read();
        }, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _session?.Dispose();
        _session = null;
        _connected = false;
    }
}