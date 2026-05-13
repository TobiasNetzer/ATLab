using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class CommunicationFactory : ICommunicationFactory
{
    public ICommunication CreateSerial(string portName, DeviceConfiguration config)
        => new SerialPortService(
            portName,
            config.BaudRate,
            config.DataBits,
            config.Parity,
            config.StopBits,
            config.Handshake,
            config.FramingMode,
            config.FramingTimeoutMs,
            config.SerialTerminationMode);

    public ICommunication CreateVisa(string resourceString, DeviceConfiguration config)
        => new NiVisaService(
            resourceString,
            config.VisaTimeoutMs,
            config.VisaTerminationMode);
}