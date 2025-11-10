using System;
using System.Text;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Services;

public class GetCommands
{
    private readonly CTIACommunication _CTIA;
    public GetCommands(CTIACommunication cTIA) => _CTIA = cTIA;

    public async Task<OperationResult<ushort>> GetDeviceID()
    {
        CommandFrame frame = new CommandFrame
        {
            Command = (ushort)GetCmd.GET_DEVICE_ID
        };

        CommandFrame responseFrame = await _CTIA.SendCommandAsync(frame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_DEVICE_ID)
            return OperationResult<ushort>.Success(BitConverter.ToUInt16(responseFrame.Payload, 0));
        else
            return OperationResult<ushort>.Failure($"Unexpected response: {responseFrame.Command}", (RespCmd)responseFrame.Command);
    }

    public async Task<OperationResult<string>> GetFirmwareVersion()
    {
        CommandFrame frame = new CommandFrame
        {
            Command = (ushort)GetCmd.GET_FW_VESION
        };

        CommandFrame responseFrame = await _CTIA.SendCommandAsync(frame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_FW_VERSION)
            return OperationResult<string>.Success(Encoding.ASCII.GetString(responseFrame.Payload));
        else
            return OperationResult<string>.Failure($"Unexpected response: {responseFrame.Command}", (RespCmd)responseFrame.Command);
    }

    public async Task<OperationResult<string>> GetFirmwareBuildDate()
    {
        CommandFrame frame = new CommandFrame
        {
            Command = (ushort)GetCmd.GET_FW_BUILD_DATE
        };

        CommandFrame responseFrame = await _CTIA.SendCommandAsync(frame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_FW_BUILD_DATE)
            return OperationResult<string>.Success(Encoding.ASCII.GetString(responseFrame.Payload));
        else
            return OperationResult<string>.Failure($"Unexpected response: {responseFrame.Command}", (RespCmd)responseFrame.Command);
    }

    public async Task<OperationResult<string>> GetFirmwareBuildTime()
    {
        CommandFrame frame = new CommandFrame
        {
            Command = (ushort)GetCmd.GET_FW_BUILD_TIME
        };

        CommandFrame responseFrame = await _CTIA.SendCommandAsync(frame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_FW_BUILD_TIME)
            return OperationResult<string>.Success(Encoding.ASCII.GetString(responseFrame.Payload));
        else
            return OperationResult<string>.Failure($"Unexpected response: {responseFrame.Command}", (RespCmd)responseFrame.Command);
    }

    public async Task<OperationResult<string>> GetDeviceName()
    {
        CommandFrame frame = new CommandFrame
        {
            Command = (ushort)GetCmd.GET_DEVICE_NAME
        };

        CommandFrame responseFrame = await _CTIA.SendCommandAsync(frame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_DEVICE_NAME)
            return OperationResult<string>.Success(Encoding.ASCII.GetString(responseFrame.Payload));
        else
            return OperationResult<string>.Failure($"Unexpected response: {responseFrame.Command}", (RespCmd)responseFrame.Command);
    }
}

