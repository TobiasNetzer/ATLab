using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.Records;

namespace ATLab.Services;

public class TestHardwareSimulator : ITestHardware
{
    public IHardwareInfo HardwareInfo { get; }
    
    public bool[] StimChannelStates {get; set;}
    public bool[] ExtStimChannelStates { get; set; }
    public bool[] MeasChannelStates { get; set; }
    public byte ActiveMeasChannelH { get; set; }
    public byte ActiveMeasChannelL { get; set; }
    public byte UseExternalProbe { get; set; }

    public TestHardwareSimulator()
    {
        HardwareInfo = new DummyHardwareInfo();
        StimChannelStates = new  bool[HardwareInfo.StimChannelCount];
        ExtStimChannelStates = new  bool[HardwareInfo.ExtStimChannelCount];
        MeasChannelStates = new  bool[HardwareInfo.MeasChannelCount];
        
        ActiveMeasChannelH = 0;
        ActiveMeasChannelL = 0;
        UseExternalProbe = 0;
    }

    public async Task<OperationResult> InitializeAsync()
    {
      await Task.CompletedTask;
        return OperationResult.Success();
    }

    public async Task<OperationResult> UpdateRelayStates()
    {
        await Task.CompletedTask;
        return OperationResult.Success();
    }

    public async Task<OperationResult> ClearRelayStates()
    {
        await Task.CompletedTask;
        return OperationResult.Success();
    }
    
    public async Task<OperationResult> ConfigureI2CInterface(I2CSpeedMode speedMode)
    {
        await Task.CompletedTask;
        return OperationResult.Success();
    }
    
    public async Task<OperationResult> ConfigureUartInterface(int baudRate, int dataBits, SerialParity parity, SerialStopBits stopBits)
    {
        await Task.CompletedTask;
        return OperationResult.Success();
    }

    public async Task<OperationResult<TestHardwareDiagnostics>> ExecuteSelfTest()
    {
        await Task.CompletedTask;
        return OperationResult<TestHardwareDiagnostics>.Success(new TestHardwareDiagnostics());
    }

    public async Task<OperationResult<I2CResponse>> ExecuteI2CTransmit(byte deviceAddr, byte[] data, int timeoutMs = 1000)
    {
        await Task.CompletedTask;
        return OperationResult<I2CResponse>.Success(new I2CResponse(true));
    }

    public async Task<OperationResult<I2CResponse>> ExecuteI2CReceive(byte deviceAddr, byte bytesToRead, int timeoutMs = 1000)
    {
        await Task.CompletedTask;
        return OperationResult<I2CResponse>.Success(new I2CResponse(true, [(byte)'0']));
    }
    
    public async Task<OperationResult<byte[]>> ExecuteUartTransceive(byte[] data, byte bytesToRead, int timeoutMs = 1000)
    {
        await Task.CompletedTask;
        return OperationResult<byte[]>.Success([(byte)'0']);
    }
}