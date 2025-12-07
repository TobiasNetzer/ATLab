using ATLab.Services;
using System.Threading.Tasks;
using ATLab.Models;
using System;
using System.Collections;
using System.Text;

namespace ATLab.CTIA;

// -------------------------
// Status codes
// -------------------------
public enum CTIAStatus : ushort
{
    CTIA_SUCCESS,
    CTIA_FAIL,
    CTIA_TOO_FEW_BYTES,
    CTIA_TOO_MANY_BYTES,
    CTIA_CRC_MISMATCH,
    CTIA_INVALID_CMD,
    CTIA_INVALID_PARAMETER,
    CTIA_TIMEOUT,
    CTIA_BUSY,
    CTIA_UNAVAILABLE
}

// -------------------------
// Response commands
// -------------------------
public enum RespCmd : ushort
{
    RESP_OK = 0x0101,
    RESP_ERROR,
    RESP_DEVICE_ID,
    RESP_DEVICE_NAME,
    RESP_FW_BUILD_DATE,
    RESP_FW_BUILD_TIME,
    RESP_FW_VERSION,
    RESP_BITFIELD_MEAS_H,
    RESP_BITFIELD_MEAS_L,
    RESP_BITFIELD_STIM,
    RESP_BITFIELD_EXT_STIM,
    RESP_EXT_PROBE_IN_STATE,
    RESP_EXT_TRIGGER_STATE,
    RESP_END = 0x01FF
}

// -------------------------
// Set commands
// -------------------------
public enum SetCmd : ushort
{
    SET_EXCLUSIVE_MEAS_H_CH = 0x0201,
    SET_MEAS_H_CH,
    SET_BITFIELD_MEAS_H_CH,
    SET_EXCLUSIVE_MEAS_L_CH,
    SET_MEAS_L_CH,
    SET_BITFIELD_MEAS_L_CH,
    SET_EXCLUSIVE_STIM_CH,
    SET_STIM_CH,
    SET_BITFIELD_STIM_CH,
    SET_EXT_STIM_CH,
    SET_BITFIELD_EXT_STIM_CH,
    SET_EXT_PROBE_IN,
    SET_EXT_TRIGGER,
    SET_END = 0x02FF
}

// -------------------------
// Clear commands
// -------------------------
public enum ClrCmd : ushort
{
    CLR_MEAS_H_CH = 0x0301,
    CLR_MEAS_H,
    CLR_MEAS_L_CH,
    CLR_MEAS_L,
    CLR_STIM_CH,
    CLR_STIM,
    CLR_EXT_STIM_CH,
    CLR_EXT_STIM,
    CLR_END = 0x03FF
}

// -------------------------
// Get commands
// -------------------------
public enum GetCmd : ushort
{
    GET_DEVICE_ID = 0x0401,
    GET_DEVICE_NAME,
    GET_FW_BUILD_DATE,
    GET_FW_BUILD_TIME,
    GET_FW_VESION,
    GET_BITFIELD_MEAS_H,
    GET_BITFIELD_MEAS_L,
    GET_BITFIELD_STIM,
    GET_BITFIELD_EXT_STIM,
    GET_EXT_PROBE_IN_STATE,
    GET_EXT_TRIGGER_STATE,
    GET_AVAILABLE_MEAS_CH,
    GET_AVAILABLE_STIM_CH,
    GET_AVAILABLE_EXT_STIM_CH,
    GET_END = 0x04FF
}

// -------------------------
// Config commands
// -------------------------
public enum ConfCmd : ushort
{
    CONF_AVAILABLE_MEAS_CH = 0x0501,
    CONF_AVAILABLE_STIM_CH,
    CONF_AVAILABLE_EXT_STIM_CH,
    CONF_AVAILABLE_UART,
    CONF_AVAILABLE_I2C,
    CONF_AVAILABLE_RS485,
    CONF_END = 0x05FF
}

// -------------------------
// Execute commands
// -------------------------
public enum ExecCmd : ushort
{
    UART_TRANSMIT = 0x0601,
    EXEC_END = 0x06FF
}

// -------------------------
// Debug commands
// -------------------------
public enum DbgCmd : ushort
{
    EXAMPLE_DBG_CMD = 0x0701,
    DBG_END = 0x07FF
}

public class CtiaCommand
{
    private readonly CtiaCommunication _CTIA;
    public CtiaCommand(CtiaCommunication cTIA) => _CTIA = cTIA;
    
    public async Task<OperationResult<bool>> SetExclusiveMeasChH(byte channel)
    {
        CtiaCommandFrame frame = new CtiaCommandFrame
        {
            Command = (ushort)SetCmd.SET_EXCLUSIVE_MEAS_H_CH,
            PayloadSize = 1,
            Payload = [channel]
        };

        CtiaCommandFrame responseFrame = await _CTIA.SendCommandAsync(frame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return OperationResult<bool>.Success(true);
        else
            return OperationResult<bool>.Failure($"Unexpected response: {responseFrame.Command}");
    }
    
    public async Task<OperationResult<bool>> SetExclusiveMeasChL(byte channel)
    {
        CtiaCommandFrame frame = new CtiaCommandFrame
        {
            Command = (ushort)SetCmd.SET_EXCLUSIVE_MEAS_L_CH,
            PayloadSize = 1,
            Payload = [channel]
        };

        CtiaCommandFrame responseFrame = await _CTIA.SendCommandAsync(frame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return OperationResult<bool>.Success(true);
        else
            return OperationResult<bool>.Failure($"Unexpected response: {responseFrame.Command}");
    }
    
    public async Task<OperationResult<bool>> SetExtStimCh(byte channel)
    {
        CtiaCommandFrame frame = new CtiaCommandFrame
        {
            Command = (ushort)SetCmd.SET_EXT_STIM_CH,
            PayloadSize = 1,
            Payload = [channel]
        };

        CtiaCommandFrame responseFrame = await _CTIA.SendCommandAsync(frame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return OperationResult<bool>.Success(true);
        else
            return OperationResult<bool>.Failure($"Unexpected response: {responseFrame.Command}");
    }
    
    public async Task<OperationResult<bool>> SetStimChBiftield(bool[] states)
    {
        if (states.Length % 8 != 0)
            throw new ArgumentException("Length must be a multiple of 8");

        int bytes = states.Length / 8;
        byte[] array = new byte[bytes];

        for (int i = 0; i < states.Length; i++)
        {
            if (states[i])
            {
                int byteIndex = i / 8;       // which byte
                int bitIndex = i % 8;        // which bit inside that byte
                array[byteIndex] |= (byte)(1 << bitIndex);
            }
        }
        
        CtiaCommandFrame frame = new CtiaCommandFrame
        {
            Command = (ushort)SetCmd.SET_BITFIELD_STIM_CH,
            PayloadSize = Convert.ToByte(array.Length),
        };
        frame.Payload = array;

        CtiaCommandFrame responseFrame = await _CTIA.SendCommandAsync(frame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return OperationResult<bool>.Success(true);
        else
            return OperationResult<bool>.Failure($"Unexpected response: {responseFrame.Command}");
    }
    
    public async Task<OperationResult<bool>> SetExtStimChBiftield(bool[] states)
    {

        int bytes = 1 + (states.Length / 8);
        byte[] array = new byte[bytes];

        for (int i = 0; i < states.Length; i++)
        {
            if (states[i])
            {
                int byteIndex = i / 8;       // which byte
                int bitIndex = i % 8;        // which bit inside that byte
                array[byteIndex] |= (byte)(1 << bitIndex);
            }
        }
        
        CtiaCommandFrame frame = new CtiaCommandFrame
        {
            Command = (ushort)SetCmd.SET_BITFIELD_EXT_STIM_CH,
            PayloadSize = Convert.ToByte(1 + (array.Length / 8)),
        };
        frame.Payload = array;

        CtiaCommandFrame responseFrame = await _CTIA.SendCommandAsync(frame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return OperationResult<bool>.Success(true);
        else
            return OperationResult<bool>.Failure($"Unexpected response: {responseFrame.Command}");
    }

    public async Task<OperationResult<ushort>> GetDeviceID()
    {
        CtiaCommandFrame frame = new CtiaCommandFrame
        {
            Command = (ushort)GetCmd.GET_DEVICE_ID
        };

        CtiaCommandFrame responseFrame = await _CTIA.SendCommandAsync(frame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_DEVICE_ID)
            return OperationResult<ushort>.Success(BitConverter.ToUInt16(responseFrame.Payload, 0));
        else
            return OperationResult<ushort>.Failure($"Unexpected response: {responseFrame.Command}");
    }

    public async Task<OperationResult<string>> GetFirmwareVersion()
    {
        CtiaCommandFrame frame = new CtiaCommandFrame
        {
            Command = (ushort)GetCmd.GET_FW_VESION
        };

        CtiaCommandFrame responseFrame = await _CTIA.SendCommandAsync(frame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_FW_VERSION)
            return OperationResult<string>.Success(Encoding.ASCII.GetString(responseFrame.Payload));
        else
            return OperationResult<string>.Failure($"Unexpected response: {responseFrame.Command}");
    }

    public async Task<OperationResult<string>> GetFirmwareBuildDate()
    {
        CtiaCommandFrame frame = new CtiaCommandFrame
        {
            Command = (ushort)GetCmd.GET_FW_BUILD_DATE
        };

        CtiaCommandFrame responseFrame = await _CTIA.SendCommandAsync(frame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_FW_BUILD_DATE)
            return OperationResult<string>.Success(Encoding.ASCII.GetString(responseFrame.Payload));
        else
            return OperationResult<string>.Failure($"Unexpected response: {responseFrame.Command}");
    }

    public async Task<OperationResult<string>> GetFirmwareBuildTime()
    {
        CtiaCommandFrame frame = new CtiaCommandFrame
        {
            Command = (ushort)GetCmd.GET_FW_BUILD_TIME
        };

        CtiaCommandFrame responseFrame = await _CTIA.SendCommandAsync(frame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_FW_BUILD_TIME)
            return OperationResult<string>.Success(Encoding.ASCII.GetString(responseFrame.Payload));
        else
            return OperationResult<string>.Failure($"Unexpected response: {responseFrame.Command}");
    }

    public async Task<OperationResult<string>> GetDeviceName()
    {
        CtiaCommandFrame frame = new CtiaCommandFrame
        {
            Command = (ushort)GetCmd.GET_DEVICE_NAME
        };

        CtiaCommandFrame responseFrame = await _CTIA.SendCommandAsync(frame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_DEVICE_NAME)
            return OperationResult<string>.Success(Encoding.ASCII.GetString(responseFrame.Payload));
        else
            return OperationResult<string>.Failure($"Unexpected response: {responseFrame.Command}");
    }
    
    public async Task<OperationResult<bool>> ClearExtStimCh(byte channel)
    {
        CtiaCommandFrame frame = new CtiaCommandFrame
        {
            Command = (ushort)ClrCmd.CLR_EXT_STIM_CH,
            PayloadSize = 1,
            Payload = [channel]
        };

        CtiaCommandFrame responseFrame = await _CTIA.SendCommandAsync(frame);

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return OperationResult<bool>.Success(true);
        else
            return OperationResult<bool>.Failure($"Unexpected response: {responseFrame.Command}");
    }
}