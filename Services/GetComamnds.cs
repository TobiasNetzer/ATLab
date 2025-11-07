using System;
using System.Text;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Services;

public class GetCommands
{
    private readonly CTIACommunication _CTIA;
    public GetCommands(CTIACommunication cTIA) => _CTIA = cTIA;
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
        getIDFrame.Command = (ushort)GetCmd.GET_DEVICE_NAME;

        CTIACommandFrame responseFrame = await _CTIA.SendCommandAsync(getIDFrame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_DEVICE_NAME)
            return Encoding.ASCII.GetString(responseFrame.Payload);
        else
            return string.Empty;
    }
}

