using System.Threading.Tasks;
using ATLab.Models;
using System;
using System.Linq;
using System.Text;

namespace ATLab.CTIA;

// -------------------------
// Status codes
// -------------------------
public enum CtiaStatus : byte
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
    RESP_SERIAL_NUMBER,
    RESP_FW_BUILD_DATE,
    RESP_FW_BUILD_TIME,
    RESP_FW_VERSION,
    RESP_AVAILABLE_MEAS_CH,
    RESP_AVAILABLE_STIM_CH,
    RESP_AVAILABLE_EXT_STIM_CH,
    RESP_BITFIELD_MEAS_H,
    RESP_BITFIELD_MEAS_L,
    RESP_BITFIELD_STIM,
    RESP_BITFIELD_EXT_STIM,
    RESP_EXT_PROBE_IN_STATE,
    RESP_EXT_TRIGGER_STATE,
    RESP_EXECUTE_SELFTEST,
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
    SET_ANALOG_BUS_DETECT,
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
    CLR_ALL_RELAYS,
    CLR_END = 0x03FF
}

// -------------------------
// Get commands
// -------------------------
public enum GetCmd : ushort
{
    GET_DEVICE_ID = 0x0401,
    GET_DEVICE_NAME,
    GET_SERIAL_NUMBER,
    GET_FW_BUILD_DATE,
    GET_FW_BUILD_TIME,
    GET_FW_VERSION,
    GET_AVAILABLE_MEAS_CH,
    GET_AVAILABLE_STIM_CH,
    GET_AVAILABLE_EXT_STIM_CH,
    GET_BITFIELD_MEAS_H,
    GET_BITFIELD_MEAS_L,
    GET_BITFIELD_STIM,
    GET_BITFIELD_EXT_STIM,
    GET_EXT_PROBE_IN_STATE,
    GET_EXT_TRIGGER_STATE,
    GET_END = 0x04FF
}

// -------------------------
// Config commands
// -------------------------
public enum ConfCmd : ushort
{
    CONF_SERIAL_NUMBER = 0x0501,
    CONF_AVAILABLE_MEAS_CH,
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
    EXECUTE_SELFTEST = 0x0601,
    UART_TRANSMIT,
    EXEC_END = 0x06FF
}

// -------------------------
// Debug commands
// -------------------------
public enum DbgCmd : ushort
{
    DBG_ENTER_BOOTLOADER = 0x0701,
    DBG_END = 0x07FF
}

public class CtiaCommand
{
    private readonly CtiaCommunication _ctia;
    public CtiaCommand(CtiaCommunication cTia) => _ctia = cTia;

    #region SET_CMD
    
    public async Task<OperationResult<bool>> SetExclusiveMeasChH(byte channel)
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)SetCmd.SET_EXCLUSIVE_MEAS_H_CH,
            PayloadSize = 1,
            Payload = [channel]
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<bool>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return OperationResult<bool>.Success(true);
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<bool>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    public async Task<OperationResult<bool>> SetExclusiveMeasChL(byte channel)
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)SetCmd.SET_EXCLUSIVE_MEAS_L_CH,
            PayloadSize = 1,
            Payload = [channel]
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<bool>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return OperationResult<bool>.Success(true);
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<bool>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    public async Task<OperationResult<bool>> SetExternalProbeIn(byte state)
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)SetCmd.SET_EXT_PROBE_IN,
            PayloadSize = 1,
            Payload = [state]
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<bool>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return OperationResult<bool>.Success(true);
        
        var status = (CtiaStatus)responseFrame.Payload[0];

        return OperationResult<bool>.Failure(status == CtiaStatus.CTIA_UNAVAILABLE
            ? "External Probe not detected."
            : $"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    public async Task<OperationResult<bool>> SetStimChBitfield(bool[] states)
    {
        if (states.Length % 8 != 0)
            return OperationResult<bool>.Failure("Length must be a multiple of 8");

        var bytes = states.Length / 8;
        var array = new byte[bytes];

        for (var i = 0; i < states.Length; i++)
        {
            if (!states[i]) continue;
            
            var byteIndex = i / 8;       // which byte
            var bitIndex = i % 8;        // which bit inside that byte
            array[byteIndex] |= (byte)(1 << bitIndex);
        }
        
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)SetCmd.SET_BITFIELD_STIM_CH,
            PayloadSize = Convert.ToByte(array.Length),
            Payload = array
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<bool>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return OperationResult<bool>.Success(true);
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<bool>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    public async Task<OperationResult<bool>> SetExtStimChBitfield(bool[] states)
    {

        var bytes = 1 + (states.Length / 8);
        var array = new byte[bytes];

        for (var i = 0; i < states.Length; i++)
        {
            if (!states[i]) continue;
            
            var byteIndex = i / 8;       // which byte
            var bitIndex = i % 8;        // which bit inside that byte
            array[byteIndex] |= (byte)(1 << bitIndex);
        }
        
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)SetCmd.SET_BITFIELD_EXT_STIM_CH,
            PayloadSize = Convert.ToByte(1 + (array.Length / 8)),
            Payload = array
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<bool>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return OperationResult<bool>.Success(true);
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<bool>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    #endregion

    #region GET_CMD

    public async Task<OperationResult<ushort>> GetDeviceID()
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)GetCmd.GET_DEVICE_ID
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<ushort>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_DEVICE_ID)
            return OperationResult<ushort>.Success(BitConverter.ToUInt16(responseFrame.Payload, 0));
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<ushort>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }

    public async Task<OperationResult<string>> GetFirmwareVersion()
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)GetCmd.GET_FW_VERSION
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<string>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_FW_VERSION)
            return OperationResult<string>.Success(Encoding.ASCII.GetString(responseFrame.Payload));
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<string>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }

    public async Task<OperationResult<string>> GetFirmwareBuildDate()
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)GetCmd.GET_FW_BUILD_DATE
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<string>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_FW_BUILD_DATE)
            return OperationResult<string>.Success(Encoding.ASCII.GetString(responseFrame.Payload));
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<string>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }

    public async Task<OperationResult<string>> GetFirmwareBuildTime()
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)GetCmd.GET_FW_BUILD_TIME
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<string>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_FW_BUILD_TIME)
            return OperationResult<string>.Success(Encoding.ASCII.GetString(responseFrame.Payload));
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<string>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }

    public async Task<OperationResult<string>> GetDeviceName()
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)GetCmd.GET_DEVICE_NAME
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<string>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_DEVICE_NAME)
            return OperationResult<string>.Success(Encoding.ASCII.GetString(responseFrame.Payload));
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<string>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    public async Task<OperationResult<string>> GetSerialNumber()
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)GetCmd.GET_SERIAL_NUMBER
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<string>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_SERIAL_NUMBER)
        {
            var serial = BitConverter.ToUInt32(responseFrame.Payload, 0);
            return OperationResult<string>.Success(serial.ToString());
        }
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<string>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    public async Task<OperationResult<int>> GetMeasChannelCount()
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)GetCmd.GET_AVAILABLE_MEAS_CH
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<int>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_AVAILABLE_MEAS_CH)
            return OperationResult<int>.Success(responseFrame.Payload[0]);
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<int>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    public async Task<OperationResult<int>> GetStimChannelCount()
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)GetCmd.GET_AVAILABLE_STIM_CH
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<int>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_AVAILABLE_STIM_CH)
            return OperationResult<int>.Success(responseFrame.Payload[0]);
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<int>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    public async Task<OperationResult<int>> GetExtStimChannelCount()
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)GetCmd.GET_AVAILABLE_EXT_STIM_CH
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<int>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_AVAILABLE_EXT_STIM_CH)
            return OperationResult<int>.Success(responseFrame.Payload[0]);
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<int>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    #endregion

    #region CLR_CMD

    public async Task<OperationResult<bool>> ClrMeasH()
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)ClrCmd.CLR_MEAS_H
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<bool>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return OperationResult<bool>.Success(true);
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<bool>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    public async Task<OperationResult<bool>> ClrMeasL()
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)ClrCmd.CLR_MEAS_L
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<bool>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return OperationResult<bool>.Success(true);
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<bool>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    public async Task<OperationResult<bool>> ClrAllRelayStates()
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)ClrCmd.CLR_ALL_RELAYS
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<bool>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return OperationResult<bool>.Success(true);
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<bool>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    #endregion
    
    #region EXEC_CMD
    
    public async Task<OperationResult<TestHardwareDiagnostics>> ExecuteSelfTest()
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)ExecCmd.EXECUTE_SELFTEST
        };

        var responseFrame = await _ctia.SendCommandAsync(frame, 10000);
        
        if (responseFrame is null)
            return OperationResult<TestHardwareDiagnostics>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command != RespCmd.RESP_EXECUTE_SELFTEST)
        {
            return OperationResult<TestHardwareDiagnostics>.Failure((CtiaStatus)responseFrame.Payload[0] == CtiaStatus.CTIA_FAIL
                ? "Analog Bus is permanently shorted. Unable to detect defective relays."
                : $"Unexpected response: CMD:{responseFrame.Command:X4}");
        }
        

        // ---------------------------------------------------------
        // Decode payload: first half = H, second half = L
        // ---------------------------------------------------------

        var total = responseFrame.PayloadSize;
        if (total % 2 != 0)
            return OperationResult<TestHardwareDiagnostics>.Failure("Invalid payload size for selftest bitfields.");

        var half = total / 2;

        var defectiveH = responseFrame.Payload.Take(half).ToArray();
        var defectiveL = responseFrame.Payload.Skip(half).Take(half).ToArray();

        var diagnostics = new TestHardwareDiagnostics();

        // ---------------------------------------------------------
        // Parse high‑side relays K201, K202, ...
        // ---------------------------------------------------------
        for (var byteIndex = 0; byteIndex < defectiveH.Length; byteIndex++)
        {
            var b = defectiveH[byteIndex];

            for (var bit = 0; bit < 8; bit++)
            {
                if ((b & (1 << bit)) == 0) continue;
                
                var relayNumber = byteIndex * 8 + bit + 1; // 1‑based
                diagnostics.DefectiveRelaysMatrixH.Add($"K2{relayNumber:00}");
            }
        }

        // ---------------------------------------------------------
        // Parse low‑side relays K301, K302, ...
        // ---------------------------------------------------------
        for (var byteIndex = 0; byteIndex < defectiveL.Length; byteIndex++)
        {
            var b = defectiveL[byteIndex];

            for (var bit = 0; bit < 8; bit++)
            {
                if ((b & (1 << bit)) == 0) continue;
                
                var relayNumber = byteIndex * 8 + bit + 1; // 1‑based
                diagnostics.DefectiveRelaysMatrixL.Add($"K3{relayNumber:00}");
            }
        }

        return OperationResult<TestHardwareDiagnostics>.Success(diagnostics);
    }
    
    #endregion
}