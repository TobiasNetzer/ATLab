using ATLab.Models;

namespace ATLab.Interfaces;

public interface ICommunicationFactory
{
    ICommunication CreateSerial(string portName, DeviceConfiguration config);
    ICommunication CreateVisa(string resource, DeviceConfiguration config);
    ICommunication CreateTcp(string ipAddress, DeviceConfiguration config);
}