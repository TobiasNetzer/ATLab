using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class SerialPortService : ICommunication, IDisposable
{
    private readonly SerialPort _port;
    private readonly object _lock = new();
    private TaskCompletionSource<byte[]>? _pendingTcs;
    private readonly Queue<byte[]> _incomingQueue = new();
    private readonly IMessageFramer _framer;
    private readonly List<byte> _rxBuffer = new();


    private bool _disposed;

    public string Resource => _port.PortName;
    public bool IsConnected => _port.IsOpen;

    public SerialPortService(
        string portName,
        int baudRate,
        int dataBits,
        Parity parity,
        StopBits stopBits,
        Handshake handshake,
        MessageFramingMode framingMode,
        int framingTimeoutMs = 100)
    {
        _framer = framingMode switch
        {
            MessageFramingMode.LF_TERMINATED => new LfMessageFramer(),
            MessageFramingMode.CHUNK => new ChunkMessageFramer(),
            MessageFramingMode.CR_LF_TERMINATED => new CrLfMessageFramer(),
            MessageFramingMode.TIMEOUT_BASED => new TimeoutMessageFramer(framingTimeoutMs, _rxBuffer, _lock, ProcessTimeoutMessage),
            _ => new ChunkMessageFramer()
        };
        
        _port = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
        {
            Handshake = handshake,
            ReadTimeout = 1000,
            WriteTimeout = 1000
        };

        _port.DataReceived += SerialPort_DataReceived;
    }

    public async Task<OperationResult> ConnectAsync()
    {
        if (_port.IsOpen)
            return OperationResult.Success();
        
        try
        {
            await Task.Run(() => _port.Open());
            return OperationResult.Success();
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult.Failure($"Access denied: {ex.Message}");
        }
        catch (IOException ex)
        {
            return OperationResult.Failure($"I/O error: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            return OperationResult.Failure($"Invalid argument: {ex.Message}");
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Unexpected error: {ex.Message}");
        }
    }
    
    public async Task<OperationResult> DisconnectAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    // Cancel any pending receive
                    if (_pendingTcs != null && !_pendingTcs.Task.IsCompleted)
                    {
                        _pendingTcs.TrySetCanceled();
                        _pendingTcs = null;
                    }

                    _incomingQueue.Clear();

                    if (_port.IsOpen)
                    {
                        _port.Close();
                    }
                }
            });

            return OperationResult.Success();
        }
        catch (IOException ex)
        {
            return OperationResult.Failure($"I/O error while disconnecting: {ex.Message}");
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Unexpected error while disconnecting: {ex.Message}");
        }
    }

    public async Task<OperationResult> ReconnectAsync()
    {
        try
        {
            if (_port.IsOpen)
            {
                _port.Close();
            }
            
            await Task.Delay(100);
            
            return await ConnectAsync();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Reconnect failed: {ex.Message}");
        }
    }

    private void ProcessTimeoutMessage()
    {
        lock (_lock)
        {
            if (_rxBuffer.Count == 0) return;

            var msg = _rxBuffer.ToArray();
            _rxBuffer.Clear();

            if (_pendingTcs != null)
            {
                _pendingTcs.TrySetResult(msg);
                _pendingTcs = null;
            }
            else
            {
                _incomingQueue.Enqueue(msg);
            }
        }
    }

    private void SerialPort_DataReceived(object? sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            var bytesToRead = _port.BytesToRead;
            if (bytesToRead <= 0) return;

            var buffer = new byte[bytesToRead];
            var read = _port.Read(buffer, 0, bytesToRead);
            if (read <= 0) return;

            lock (_lock)
            {
                _rxBuffer.AddRange(buffer);

                if (!_framer.TryExtractMessages(_rxBuffer, out var messages))
                    return;
                
                foreach (var msg in messages)
                {
                    if (_pendingTcs != null)
                    {
                        _pendingTcs.TrySetResult(msg);
                        _pendingTcs = null;
                    }
                    else
                    {
                        _incomingQueue.Enqueue(msg);
                    }
                }
            }
        }
        catch
        {
            // optional logging
        }
    }


    public void SendRaw(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        _port.Write(data, 0, data.Length);
    }

    public async Task<byte[]> SendAsync(byte[] data, int timeoutMs = 1000)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        Task<byte[]> waitTask;
        lock (_lock)
        {
            if (_pendingTcs != null)
                throw new InvalidOperationException("Another request is already pending.");

            _pendingTcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            waitTask = _pendingTcs.Task;
        }

        try
        {
            _port.Write(data, 0, data.Length);

            using var cts = new CancellationTokenSource(timeoutMs);
            var completed = await Task.WhenAny(waitTask, Task.Delay(Timeout.Infinite, cts.Token));
            if (completed == waitTask)
            {
                return await waitTask;
            }
            else
            {
                lock (_lock) { _pendingTcs = null; }
                throw new TimeoutException($"Timed out waiting for response from device ({_port.PortName}).");
            }
        }
        finally
        {
            lock (_lock)
            {
                if (_pendingTcs != null && !_pendingTcs.Task.IsCompleted)
                {
                    _pendingTcs.TrySetException(new TimeoutException("SendAsync aborted."));
                    _pendingTcs = null;
                }
            }
        }
    }

    public Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_incomingQueue.Count > 0)
            {
                return Task.FromResult(_incomingQueue.Dequeue());
            }

            if (_pendingTcs != null)
                throw new InvalidOperationException("A request is already pending.");

            _pendingTcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() =>
            {
                lock (_lock)
                {
                    _pendingTcs?.TrySetCanceled();
                    _pendingTcs = null;
                }
            });

            return _pendingTcs.Task;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _port.DataReceived -= SerialPort_DataReceived;
        if (_framer is IDisposable disposableFramer) disposableFramer.Dispose();
        if (_port.IsOpen) _port.Close();
        _port.Dispose();
    }
}