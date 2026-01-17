using System.IO.Ports;

namespace ATLab.Models;

public class DeviceConfiguration
{

    // Serial settings
    public int BaudRate { get; set; } = 115200;
    public int DataBits { get; set; } = 8;
    public Parity Parity { get; set; } = Parity.None;
    public StopBits StopBits { get; set; } = StopBits.One;
    public Handshake Handshake { get; set; } = Handshake.None;

    // VISA settings
    public int VisaTimeoutMs { get; set; } = 2000;
    public byte VisaTerminationChar { get; set; } = 0x0A; // LF
    
}
