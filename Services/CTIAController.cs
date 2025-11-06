using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;
using ATLab.Interfaces;

namespace ATLab.Services;

public class CTIAController : IDisposable
{
    public GetCommands? Get { get; private set; }

    public string FirmwareVersion { get; private set; } = string.Empty;
    public string DeviceName { get; private set; } = string.Empty;
    public string BuildDate { get; private set; } = string.Empty;
    public string BuildTime { get; private set; } = string.Empty;

    private RelayState? _Meas_H { get; set; }
    private RelayState? _Meas_L { get; set; }
    private RelayState? _Stim { get; set; }
    private RelayState? _Ext_Stim { get; set; }

    private readonly ICommunicationInterface _serialPort;

    public string ConnectedSerialPort;

    public CTIAController(string serialPort)
    {
        ConnectedSerialPort = serialPort;
        _serialPort = new SerialPortService(serialPort);
        
    }

    public async Task InitializeAsync()
    {
        Get = new GetCommands(this);
        var DeviceId = await Get.GetDeviceID();
        if (DeviceId != 0xA101)
        {
            throw new InvalidOperationException("Device ID invalid");
        }
        FirmwareVersion = await Get.GetFirmwareVersion();
        DeviceName = await Get.GetDeviceName();
        BuildDate = await Get.GetFirmwareBuildDate();
        BuildTime = await Get.GetFirmwareBuildTime();
        // Initialize relay states
        _Meas_H = new RelayState(32);
        _Meas_L = new RelayState(32);
        _Stim = new RelayState(16);
        _Ext_Stim = new RelayState(4);
    }


    public async Task<CTIACommandFrame> SendCommandAsync(CTIACommandFrame frame, int timeoutMs = 1000)
    {
        byte[] responseBytes = await _serialPort.SendAsync(frame.ToByteArray(), timeoutMs);
        return CmdFrameParser.Parse(responseBytes);
    }

    public async Task<CTIACommandFrame> ReceiveCommandAsync(CancellationToken cancellationToken = default)
    {
        byte[] receivedData = await _serialPort.ReceiveAsync(cancellationToken);
        return CmdFrameParser.Parse(receivedData);
    }

    public void Dispose()
    {
        if (_serialPort is IDisposable d) d.Dispose();
    }
}

public class GetCommands
{
    private readonly CTIAController _CTIA;
    public GetCommands(CTIAController cTIA) => _CTIA = cTIA;
    public async Task<ushort> GetDeviceID()
    {
        CTIACommandFrame getIDFrame = new CTIACommandFrame();
        getIDFrame.Command = (ushort)GetCmd.GET_DEVICE_ID;

        CTIACommandFrame responseFrame = await _CTIA.SendCommandAsync(getIDFrame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_DEVICE_ID)
            return BitConverter.ToUInt16(responseFrame.Payload, 0);
        else
            return 0x00;
    }

    public async Task<string> GetFirmwareVersion()
    {
        CTIACommandFrame getIDFrame = new CTIACommandFrame();
        getIDFrame.Command = (ushort)GetCmd.GET_FW_VESION;

        CTIACommandFrame responseFrame = await _CTIA.SendCommandAsync(getIDFrame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_FW_VERSION)
            return Encoding.ASCII.GetString(responseFrame.Payload);
        else
            return string.Empty;
    }

    public async Task<string> GetFirmwareBuildDate()
    {
        CTIACommandFrame getIDFrame = new CTIACommandFrame();
        getIDFrame.Command = (ushort)GetCmd.GET_FW_BUILD_DATE;

        CTIACommandFrame responseFrame = await _CTIA.SendCommandAsync(getIDFrame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_FW_BUILD_DATE)
            return Encoding.ASCII.GetString(responseFrame.Payload);
        else
            return string.Empty;
    }

    public async Task<string> GetFirmwareBuildTime()
    {
        CTIACommandFrame getIDFrame = new CTIACommandFrame();
        getIDFrame.Command = (ushort)GetCmd.GET_FW_BUILD_TIME;

        CTIACommandFrame responseFrame = await _CTIA.SendCommandAsync(getIDFrame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_FW_BUILD_TIME)
            return Encoding.ASCII.GetString(responseFrame.Payload);
        else
            return string.Empty;
    }
    
    public async Task<string> GetDeviceName()
    {
        CTIACommandFrame getIDFrame = new CTIACommandFrame();
        getIDFrame.Command = (ushort) GetCmd.GET_DEVICE_NAME;

        CTIACommandFrame responseFrame = await _CTIA.SendCommandAsync(getIDFrame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_DEVICE_NAME)
            return Encoding.ASCII.GetString(responseFrame.Payload);
        else
            return string.Empty;
    }
}

