using System.IO.Ports;
using ATLab.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Models;

public partial class DeviceConfiguration : ObservableObject
{
    // Serial settings
    [ObservableProperty] private int _baudRate = 115200;
    [ObservableProperty] private int _dataBits = 8;
    [ObservableProperty] private Parity _parity = Parity.None;
    [ObservableProperty] private StopBits _stopBits = StopBits.One;
    [ObservableProperty] private Handshake _handshake = Handshake.None;

    // VISA settings
    [ObservableProperty] private int _visaTimeoutMs = 2000;
    [ObservableProperty] private VisaTerminationMode _visaTerminationMode = VisaTerminationMode.LF;
}
