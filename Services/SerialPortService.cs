using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services
{
    /// <summary>
    /// Generic serial port service for sending/receiving raw bytes.
    /// Device-specific frame parsing should be handled by the controller class.
    /// </summary>
    public class SerialPortService : ITestHardwareCommunication, IDisposable
    {
        private readonly SerialPort _port;
        private readonly object _lock = new();
        private TaskCompletionSource<byte[]>? _pendingTcs;
        private readonly Queue<byte[]> _incomingQueue = new();

        private bool _disposed;

        public string PortName => _port.PortName;

        public SerialPortService(string portName, int baudRate = 115200)
        {
            _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                Handshake = Handshake.None,
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };

            _port.DataReceived += SerialPort_DataReceived;
        }

        public OperationResult TryOpen()
        {
            try
            {
                _port.Open();
                return OperationResult.Success();
            }
            catch (UnauthorizedAccessException ex)
            {
                return OperationResult.Failure($"Access denied: {ex.Message}");
            }
            catch (System.IO.IOException ex)
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

        private void SerialPort_DataReceived(object? sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int bytesToRead = _port.BytesToRead;
                if (bytesToRead <= 0) return;

                var buffer = new byte[bytesToRead];
                int read = _port.Read(buffer, 0, bytesToRead);
                if (read <= 0) return;

                TaskCompletionSource<byte[]>? tcs = null;
                lock (_lock)
                {
                    if (_pendingTcs != null)
                    {
                        tcs = _pendingTcs;
                        _pendingTcs = null;
                    }
                    else
                    {
                        _incomingQueue.Enqueue(buffer);
                    }
                }

                tcs?.TrySetResult(buffer);
            }
            catch
            {
                // optionally log
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
                    throw new TimeoutException("Timed out waiting for response.");
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
            if (_port.IsOpen) _port.Close();
            _port.Dispose();
        }
    }
}
