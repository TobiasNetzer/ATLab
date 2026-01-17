namespace ATLab.Interfaces;

public interface ICommunicationFactory
{
    ICommunication CreateSerial(string portName, int baudRate = 115200);
}
