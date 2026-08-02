using System.Threading.Tasks;
using ATLab.Models;
using System;
using System.Linq;
using System.Text;
using ATLab.Enums;
using ATLab.Records;

namespace ATLab.CTIA;

public enum CtiaControlByte : byte {
    CONTROL_BYTE_IGNORE_CRC_BYTE = 1,
    CONTROL_BYTE_DONT_SEND_RESPONSE = 2
}

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
    RESP_TIMEOUT,
    RESP_BUSY,
    RESP_DEVICE_ID,
    RESP_DEVICE_NAME,
    RESP_SERIAL_NUMBER,
    RESP_FW_BUILD_DATE,
    RESP_FW_BUILD_TIME,
    RESP_FW_VERSION,
    RESP_AVAILABLE_MEAS_CH,
    RESP_AVAILABLE_STIM_CH,
    RESP_AVAILABLE_EXT_STIM_CH,
    RESP_AVAILABLE_I2C_INTERFACE,
    RESP_AVAILABLE_UART_INTERFACE,
    RESP_AVAILABLE_RS485_INTERFACE,
    RESP_BITFIELD_MEAS_H,
    RESP_BITFIELD_MEAS_L,
    RESP_BITFIELD_STIM,
    RESP_BITFIELD_EXT_STIM,
    RESP_EXT_PROBE_IN_STATE,
    RESP_EXT_TRIGGER_STATE,
    RESP_EXECUTE_SELFTEST,
    RESP_I2C_RECEIVE,
    RESP_I2C_NACK,
    RESP_I2C_TIMEOUT,
    RESP_UART_TRANSCEIVE,
    RESP_UART_TIMEOUT,
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
    GET_AVAILABLE_I2C_INTERFACE,
    GET_AVAILABLE_UART_INTERFACE,
    GET_AVAILABLE_RS485_INTERFACE,
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
    CONF_AVAILABLE_I2C,
    CONF_AVAILABLE_UART,
    CONF_AVAILABLE_RS485,
    CONF_I2C_SETTINGS,
    CONF_UART_SETTINGS,
    CONF_RS485_SETTINGS,
    CONF_END = 0x05FF
}

// -------------------------
// Execute commands
// -------------------------
public enum ExecCmd : ushort
{
    EXECUTE_SELFTEST = 0x0601,
    EXECUTE_I2C_TRANSMIT,
    EXECUTE_I2C_RECEIVE,
    EXECUTE_UART_TRANSCEIVE,
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
    private readonly ICtiaCommunication _ctia;
    public CtiaCommand(ICtiaCommunication cTia) => _ctia = cTia;

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
    
    public async Task<OperationResult<int>> GetI2CInterface()
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)GetCmd.GET_AVAILABLE_I2C_INTERFACE
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<int>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_AVAILABLE_I2C_INTERFACE)
            return OperationResult<int>.Success(responseFrame.Payload[0]);
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<int>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    public async Task<OperationResult<int>> GetUartInterface()
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)GetCmd.GET_AVAILABLE_UART_INTERFACE
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<int>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_AVAILABLE_UART_INTERFACE)
            return OperationResult<int>.Success(responseFrame.Payload[0]);
        
        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<int>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    public async Task<OperationResult<int>> GetRs485Interface()
    {
        var frame = new CtiaCommandFrame
        {
            Command = (ushort)GetCmd.GET_AVAILABLE_RS485_INTERFACE
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);
        
        if (responseFrame is null)
            return OperationResult<int>.Failure("Communication with test hardware failed.");

        if ((RespCmd)responseFrame.Command == RespCmd.RESP_AVAILABLE_RS485_INTERFACE)
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
    
    #region CONF_CMD

    public async Task<OperationResult> ConfI2CSettings(I2CSpeedMode  speedMode)
    {

        var frame = new CtiaCommandFrame
        {
            Command     = (ushort)ConfCmd.CONF_I2C_SETTINGS,
            PayloadSize = 1,
            Payload     = [Convert.ToByte(speedMode)]
        };

        var responseFrame = await _ctia.SendCommandAsync(frame);

        if (responseFrame is null)
            return OperationResult.Failure("Communication with test hardware failed.");
        
        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return OperationResult.Success();

        if (responseFrame.Payload.Length < 1)
            return OperationResult.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} (no status byte)");

        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    public async Task<OperationResult> ConfUartSettings(int baud, int dataBits, SerialParity parity, SerialStopBits stopBits)
    {

        var payload = new byte[7];
        
        payload[0] = (byte)(baud & 0xFF);
        payload[1] = (byte)((baud >> 8) & 0xFF);
        payload[2] = (byte)((baud >> 16) & 0xFF);
        payload[3] = (byte)((baud >> 24) & 0xFF);
        
        payload[4] = (byte)dataBits;
        
        payload[5] = stopBits switch
        {
            SerialStopBits.ONE => (byte)1,
            SerialStopBits.TWO => (byte)2,
            _ => throw new ArgumentException("Unsupported stop bits")
        };
        
        payload[6] = parity switch
        {
            SerialParity.NONE => (byte)0,
            SerialParity.EVEN => (byte)1,
            SerialParity.ODD  => (byte)2,
            _ => throw new ArgumentException("Unsupported parity")
        };
        
        var frame = new CtiaCommandFrame
        {
            Command     = (ushort)ConfCmd.CONF_UART_SETTINGS,
            PayloadSize = (byte)payload.Length,
            Payload     = payload
        };
        
        var responseFrame = await _ctia.SendCommandAsync(frame);

        if (responseFrame is null)
            return OperationResult.Failure("Communication with test hardware failed.");
        
        if ((RespCmd)responseFrame.Command == RespCmd.RESP_OK)
            return OperationResult.Success();

        if (responseFrame.Payload.Length < 1)
            return OperationResult.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} (no status byte)");

        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
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

    public async Task<OperationResult<I2CResponse>> ExecuteI2CTransmit(byte deviceAddr, byte[] data, int timeoutMs)
    {
        if (data.Length == 0)
            return OperationResult<I2CResponse>.Failure("Invalid payload.");

        var payload = new byte[data.Length + 5];

        payload[0] = deviceAddr;
        payload[1] = (byte)(timeoutMs);
        payload[2] = (byte)(timeoutMs >> 8);
        payload[3] = (byte)(timeoutMs >> 16);
        payload[4] = (byte)(timeoutMs >> 24);
        
        Array.Copy(data, 0, payload, 5, data.Length);

        var frame = new CtiaCommandFrame
        {
            Command     = (ushort)ExecCmd.EXECUTE_I2C_TRANSMIT,
            PayloadSize = (byte)payload.Length,
            Payload     = payload
        };

        var responseFrame = await _ctia.SendCommandAsync(frame, timeoutMs + 1000);

        if (responseFrame is null)
            return OperationResult<I2CResponse>.Failure("Communication with test hardware failed.");
        
        switch ((RespCmd)responseFrame.Command)
        {
            case RespCmd.RESP_OK:
                return OperationResult<I2CResponse>.Success(new I2CResponse(true));
            case RespCmd.RESP_I2C_TIMEOUT:
                return OperationResult<I2CResponse>.Timeout();
            case RespCmd.RESP_I2C_NACK:
                return OperationResult<I2CResponse>.Success(new I2CResponse(false));
        }

        if (responseFrame.Payload.Length < 1)
            return OperationResult<I2CResponse>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} (no status byte)");

        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<I2CResponse>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    public async Task<OperationResult<I2CResponse>> ExecuteI2CReceive(byte deviceAddr, byte rxSize, int timeoutMs)
    {
        if (rxSize == 0)
            return OperationResult<I2CResponse>.Failure("Invalid receive size.");

        var payload = new byte[6];

        payload[0] = deviceAddr;
        payload[1] = (byte)timeoutMs;
        payload[2] = (byte)(timeoutMs >> 8);
        payload[3] = (byte)(timeoutMs >> 16);
        payload[4] = (byte)(timeoutMs >> 24);
        payload[5] = rxSize;

        var frame = new CtiaCommandFrame
        {
            Command     = (ushort)ExecCmd.EXECUTE_I2C_RECEIVE,
            PayloadSize = (byte)payload.Length,
            Payload = payload
        };

        var responseFrame = await _ctia.SendCommandAsync(frame, timeoutMs + 1000);

        if (responseFrame is null)
            return OperationResult<I2CResponse>.Failure("Communication with test hardware failed.");

        switch ((RespCmd)responseFrame.Command)
        {
            case RespCmd.RESP_I2C_RECEIVE:
                return OperationResult<I2CResponse>.Success(new I2CResponse(true, responseFrame.Payload));
            case RespCmd.RESP_I2C_TIMEOUT:
                return OperationResult<I2CResponse>.Timeout();
            case RespCmd.RESP_I2C_NACK:
                return OperationResult<I2CResponse>.Success(new I2CResponse(false));
        }

        if (responseFrame.Payload.Length < 1)
            return OperationResult<I2CResponse>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} (no status byte)");

        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<I2CResponse>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    public async Task<OperationResult<byte[]>> ExecuteUartTransceive(byte[] data, byte rxSize, int timeoutMs)
    {
        
        var payload = new byte[data.Length + 5];

        payload[0] = rxSize;
        payload[1] = (byte)timeoutMs;
        payload[2] = (byte)(timeoutMs >> 8);
        payload[3] = (byte)(timeoutMs >> 16);
        payload[4] = (byte)(timeoutMs >> 24);
        
        Array.Copy(data, 0, payload, 5, data.Length);

        var frame = new CtiaCommandFrame
        {
            Command     = (ushort)ExecCmd.EXECUTE_UART_TRANSCEIVE,
            PayloadSize = (byte)payload.Length,
            Payload = payload
        };

        var responseFrame = await _ctia.SendCommandAsync(frame, timeoutMs + 1000);

        if (responseFrame is null)
            return OperationResult<byte[]>.Failure("Communication with test hardware failed.");

        switch ((RespCmd)responseFrame.Command)
        {
            case RespCmd.RESP_UART_TRANSCEIVE:
                return OperationResult<byte[]>.Success(responseFrame.Payload);
            case RespCmd.RESP_UART_TIMEOUT:
                return OperationResult<byte[]>.Timeout();
        }

        if (responseFrame.Payload.Length < 1)
            return OperationResult<byte[]>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} (no status byte)");

        var status = (CtiaStatus)responseFrame.Payload[0];
        return OperationResult<byte[]>.Failure($"Unexpected response: CMD:{responseFrame.Command:X4} MSG:{status}");
    }
    
    #endregion
}