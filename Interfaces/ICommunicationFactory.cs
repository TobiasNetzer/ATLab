using ATLab.Models;

namespace ATLab.Interfaces;

/// <summary>Creates the transport for a device type with the settings from its device configuration.</summary>
public interface ICommunicationFactory
{
    ICommunication CreateSerial(string portName, DeviceConfiguration config);
    ICommunication CreateVisa(string resource, DeviceConfiguration config);
    ICommunication CreateTcp(string ipAddress, DeviceConfiguration config);
}