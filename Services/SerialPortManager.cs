using System;
using System.Collections.Generic;
using ATLab.Interfaces;

namespace ATLab.Services;

public class SerialPortManager : ISerialPortManager, IDisposable
{
    private readonly Dictionary<string, SerialPortService> _ports = new();

    public ISerialCommunication GetPort(string portName)
    {
        if (string.IsNullOrEmpty(portName))
            throw new ArgumentException("Port name cannot be null or empty", nameof(portName));

        if (!_ports.TryGetValue(portName, out var service))
        {
            service = new SerialPortService(portName);
            _ports[portName] = service;
        }

        return service;
    }

    public bool IsOpen(string portName)
    {
        if (_ports.TryGetValue(portName, out var service))
        {
            return service.IsOpen;
        }
        return false;
    }

    public void Open(string portName)
    {
        var service = (SerialPortService)GetPort(portName);
        if (!service.IsOpen)
        {
            var result = service.TryOpen();
            if (!result.IsSuccess)
            {
                throw new Exception($"Failed to open port {portName}: {result.ErrorMessage}");
            }
        }
    }

    public void Dispose()
    {
        foreach (var port in _ports.Values)
        {
            port.Dispose();
        }
        _ports.Clear();
    }
}
