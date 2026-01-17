using ATLab.Models;

namespace ATLab.Interfaces;

public interface ICommunicationFactory
{
    ICommunication CreateSerial(string port, DeviceConfiguration config);
    ICommunication CreateVisa(string resource, DeviceConfiguration config);
}
