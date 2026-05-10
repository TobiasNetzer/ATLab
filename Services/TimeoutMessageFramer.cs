using System;
using System.Collections.Generic;
using System.Timers;
using ATLab.Interfaces;

namespace ATLab.Services;

public class TimeoutMessageFramer : IMessageFramer, IDisposable
{
    private readonly Timer _timer;
    private readonly List<byte> _bufferReference;
    private readonly object _lock;
    private readonly Action _onMessageReady;

    public TimeoutMessageFramer(int timeoutMs, List<byte> buffer, object lockObject, Action onMessageReady)
    {
        _bufferReference = buffer;
        _lock = lockObject;
        _onMessageReady = onMessageReady;

        _timer = new Timer(timeoutMs);
        _timer.AutoReset = false;
        _timer.Elapsed += OnTimerElapsed;
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        lock (_lock)
        {
            if (_bufferReference.Count > 0)
            {
                _onMessageReady.Invoke();
            }
        }
    }

    public bool TryExtractMessages(List<byte> buffer, out List<byte[]> messages)
    {
        messages = new List<byte[]>();
        
        _timer.Stop();
        
        if (buffer.Count > 0)
        {
            _timer.Start();
        }

        return false;
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}