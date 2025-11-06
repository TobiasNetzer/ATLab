namespace ATLab.Models;

// -------------------------
// Status codes
// -------------------------
public enum CTIAStatus : ushort
{
    CTIA_SUCCESS,
    CTIA_FAIL,
    CTIA_TOO_FEW_BYTES,
    CTIA_TOO_MANY_BYTES,
    CTIA_CRC_MISSMATCH,
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