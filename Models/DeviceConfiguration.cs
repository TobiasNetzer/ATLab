using System;
using System.IO.Ports;
using ATLab.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class DeviceConfiguration : ObservableObject
{
    // Serial settings
    [ObservableProperty]
    private int _baudRate = 115200;
    
    [ObservableProperty]
    private int _dataBits = 8;
    
    [ObservableProperty]
    private Parity _parity = Parity.None;
    
    [ObservableProperty]
    private StopBits _stopBits = StopBits.One;
    
    [ObservableProperty]
    private Handshake _handshake = Handshake.None;
    
    [ObservableProperty]
    private MessageFramingMode _framingMode = MessageFramingMode.CHUNK;
    
    [ObservableProperty]
    private int _framingTimeoutMs = 100;
    
    [ObservableProperty]
    private SerialTerminationMode _serialTerminationMode = SerialTerminationMode.NONE;

    // VISA settings
    [ObservableProperty]
    private int _visaTimeoutMs = 2000;
    
    [ObservableProperty]
    private VisaTerminationMode _visaTerminationMode = VisaTerminationMode.LF;

    // TCP settings
    [ObservableProperty]
    private int _tcpPort = 5025;

    [ObservableProperty]
    private int _tcpTimeoutMs = 2000;

    [ObservableProperty]
    private TcpTerminationMode _tcpTerminationMode = TcpTerminationMode.LF;
    
    public override bool Equals(object? obj)
    {
        if (obj is not DeviceConfiguration other)
            return false;

        return BaudRate == other.BaudRate
               && DataBits == other.DataBits
               && Parity == other.Parity
               && StopBits == other.StopBits
               && Handshake == other.Handshake
               && FramingMode == other.FramingMode
               && FramingTimeoutMs == other.FramingTimeoutMs
               && SerialTerminationMode == other.SerialTerminationMode
               && VisaTimeoutMs == other.VisaTimeoutMs
               && VisaTerminationMode == other.VisaTerminationMode
               && TcpPort == other.TcpPort
               && TcpTimeoutMs == other.TcpTimeoutMs
               && TcpTerminationMode == other.TcpTerminationMode;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(BaudRate);
        hash.Add(DataBits);
        hash.Add(Parity);
        hash.Add(StopBits);
        hash.Add(Handshake);
        hash.Add(FramingMode);
        hash.Add(FramingTimeoutMs);
        hash.Add(SerialTerminationMode);
        hash.Add(VisaTimeoutMs);
        hash.Add(VisaTerminationMode);
        hash.Add(TcpPort);
        hash.Add(TcpTimeoutMs);
        hash.Add(TcpTerminationMode);
        return hash.ToHashCode();
    }

    public DeviceConfiguration Clone()
    {
        return (DeviceConfiguration)MemberwiseClone();
    }

}