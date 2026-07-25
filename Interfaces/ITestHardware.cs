using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Models;
using ATLab.Records;

namespace ATLab.Interfaces;

public interface ITestHardware
{
    bool[] StimChannelStates {get; set; }
    bool[] ExtStimChannelStates { get; set; }
    bool[] MeasChannelStates { get; set; }
    byte ActiveMeasChannelH { get; set; }
    byte ActiveMeasChannelL { get; set; }
    byte UseExternalProbe { get; set; }
    IHardwareInfo HardwareInfo { get; }
    Task<OperationResult> InitializeAsync();
    Task<OperationResult> UpdateRelayStates();
    Task<OperationResult> ClearRelayStates();
    Task<OperationResult> ConfigureI2CInterface(I2CSpeedMode speedMode);
    Task<OperationResult> ConfigureUartInterface(int baudRate, int dataBits, SerialParity parity, SerialStopBits stopBits);
    Task<OperationResult<TestHardwareDiagnostics>> ExecuteSelfTest();
    Task<OperationResult<I2CResponse>> ExecuteI2CTransmit(byte deviceAddr, byte[] data, int timeoutMs = 1000);
    Task<OperationResult<I2CResponse>> ExecuteI2CReceive(byte deviceAddr, byte bytesToRead,  int timeoutMs = 1000);
    Task<OperationResult<byte[]>> ExecuteUartTransceive(byte[] data, byte bytesToRead, int timeoutMs = 1000);
}