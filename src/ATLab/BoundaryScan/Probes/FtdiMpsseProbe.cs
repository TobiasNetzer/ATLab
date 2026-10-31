using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ATLab.Models;

namespace ATLab.BoundaryScan.Probes;

/// <summary>
/// FTDI high speed devices (FT2232H, FT232H, FT4232H) driven in MPSSE mode through the D2XX driver.
/// Default pinout on the low byte: ADBUS0 = TCK, ADBUS1 = TDI, ADBUS2 = TDO, ADBUS3 = TMS.
/// </summary>
public sealed class FtdiMpsseProbe : IJtagProbe
{
    private const uint FtOk = 0;
    private const uint OpenBySerialNumber = 1;
    private const uint PurgeRxTx = 3;
    private const byte BitModeReset = 0x00;
    private const byte BitModeMpsse = 0x02;

    // FT_DEVICE values of the MPSSE capable high speed devices
    private static readonly HashSet<uint> MpsseDeviceTypes = [6, 7, 8];

    // MPSSE opcodes, JTAG uses LSB first, data out on the falling and in on the rising edge
    private const byte ShiftBytesOutIn = 0x39;
    private const byte ShiftBitsOutIn = 0x3B;
    private const byte ShiftTmsOutIn = 0x6B;
    private const byte SetLowByte = 0x80;
    private const byte LoopbackOff = 0x85;
    private const byte SetClockDivisor = 0x86;
    private const byte SendImmediate = 0x87;
    private const byte DisableClockDivideBy5 = 0x8A;
    private const byte DisableThreePhaseClocking = 0x8D;
    private const byte DisableAdaptiveClocking = 0x97;
    private const byte BadCommand = 0xAA;

    private const int BaseClockKHz = 60_000;
    private const int MaxBitsPerTransfer = 4096;
    private const int ReadTimeoutMs = 2000;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint CreateDeviceInfoListDelegate(out uint numDevices);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint GetDeviceInfoDetailDelegate(uint index, out uint flags, out uint type, out uint id,
        out uint locationId, byte[] serialNumber, byte[] description, out IntPtr handle);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint OpenExDelegate(byte[] arg, uint flags, out IntPtr handle);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint HandleDelegate(IntPtr handle);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint PurgeDelegate(IntPtr handle, uint mask);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint SetUsbParametersDelegate(IntPtr handle, uint inTransferSize, uint outTransferSize);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint SetTimeoutsDelegate(IntPtr handle, uint readTimeout, uint writeTimeout);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint SetLatencyTimerDelegate(IntPtr handle, byte latency);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint SetBitModeDelegate(IntPtr handle, byte mask, byte mode);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint GetQueueStatusDelegate(IntPtr handle, out uint bytesInQueue);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint ReadWriteDelegate(IntPtr handle, byte[] buffer, uint count, out uint transferred);

    private static IntPtr? _library;

    private CreateDeviceInfoListDelegate? _createDeviceInfoList;
    private GetDeviceInfoDetailDelegate? _getDeviceInfoDetail;
    private OpenExDelegate? _openEx;
    private HandleDelegate? _close;
    private HandleDelegate? _resetDevice;
    private PurgeDelegate? _purge;
    private SetUsbParametersDelegate? _setUsbParameters;
    private SetTimeoutsDelegate? _setTimeouts;
    private SetLatencyTimerDelegate? _setLatencyTimer;
    private SetBitModeDelegate? _setBitMode;
    private GetQueueStatusDelegate? _getQueueStatus;
    private ReadWriteDelegate? _read;
    private ReadWriteDelegate? _write;

    private IntPtr _handle;

    public bool IsOpen => _handle != IntPtr.Zero;

    public OperationResult<IReadOnlyList<JtagProbeInfo>> ListProbes()
    {
        var loaded = EnsureLoaded();
        if (loaded.IsFailure)
            return OperationResult<IReadOnlyList<JtagProbeInfo>>.Failure(loaded.ErrorMessage);

        if (_createDeviceInfoList!(out var count) != FtOk)
            return OperationResult<IReadOnlyList<JtagProbeInfo>>.Failure("FTDI device enumeration failed.");

        var probes = new List<JtagProbeInfo>();

        for (uint i = 0; i < count; i++)
        {
            var serial = new byte[16];
            var description = new byte[64];

            if (_getDeviceInfoDetail!(i, out _, out var type, out _, out _, serial, description, out _) != FtOk)
                continue;

            if (!MpsseDeviceTypes.Contains(type))
                continue;

            var serialText = ReadCString(serial);

            // Channels of an FTDI device that is already open somewhere else have no serial number
            if (string.IsNullOrWhiteSpace(serialText))
                continue;

            probes.Add(new JtagProbeInfo(serialText, ReadCString(description)));
        }

        return OperationResult<IReadOnlyList<JtagProbeInfo>>.Success(probes);
    }

    public OperationResult Open(string? serialNumber, JtagProbeOptions options)
    {
        if (IsOpen)
            Close();

        var loaded = EnsureLoaded();
        if (loaded.IsFailure)
            return loaded;

        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            var probes = ListProbes();
            if (probes.IsFailure)
                return OperationResult.Failure(probes.ErrorMessage);

            serialNumber = probes.Value!.FirstOrDefault()?.SerialNumber;
            if (serialNumber == null)
                return OperationResult.Failure("No FTDI MPSSE device found.");
        }

        var serialBytes = Encoding.ASCII.GetBytes(serialNumber.Trim() + "\0");

        if (_openEx!(serialBytes, OpenBySerialNumber, out var handle) != FtOk || handle == IntPtr.Zero)
            return OperationResult.Failure($"Could not open FTDI device '{serialNumber}'.");

        _handle = handle;

        var configured = ConfigureMpsse(options);
        if (configured.IsFailure)
        {
            Close();
            return configured;
        }

        return OperationResult.Success();
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        _setBitMode?.Invoke(_handle, 0, BitModeReset);
        _close?.Invoke(_handle);
        _handle = IntPtr.Zero;
    }

    public OperationResult<bool[]> Shift(bool[] tms, bool[] tdi)
    {
        if (!IsOpen)
            return OperationResult<bool[]>.Failure("FTDI device is not open.");

        if (tms.Length != tdi.Length)
            throw new ArgumentException("TMS and TDI must have the same length.");

        var tdo = new bool[tms.Length];

        for (var start = 0; start < tms.Length; start += MaxBitsPerTransfer)
        {
            var count = Math.Min(MaxBitsPerTransfer, tms.Length - start);
            var result = ShiftChunk(tms, tdi, tdo, start, count);

            if (result.IsFailure)
                return OperationResult<bool[]>.Failure(result.ErrorMessage);
        }

        return OperationResult<bool[]>.Success(tdo);
    }

    public void Dispose() => Close();

    private enum SegmentKind
    {
        TmsBit,
        DataBytes,
        DataBits
    }

    private readonly record struct Segment(SegmentKind Kind, int Start, int Count);

    private OperationResult ShiftChunk(bool[] tms, bool[] tdi, bool[] tdo, int start, int count)
    {
        var commands = new List<byte>(count);
        var segments = new List<Segment>();
        var end = start + count;
        var i = start;

        while (i < end)
        {
            // Data commands keep TMS at the level of the last TMS command, so every run of TMS = 0 starts with
            // a TMS command that pulls it low and only then continues with the faster data commands
            AddTmsBit(commands, segments, i, tms[i], tdi[i]);
            var runStart = ++i;

            if (tms[runStart - 1])
                continue;

            while (i < end && !tms[i])
                i++;

            var runLength = i - runStart;
            var byteCount = runLength / 8;
            var bitCount = runLength % 8;

            if (byteCount > 0)
            {
                commands.Add(ShiftBytesOutIn);
                commands.Add((byte)((byteCount - 1) & 0xFF));
                commands.Add((byte)((byteCount - 1) >> 8));

                for (var b = 0; b < byteCount; b++)
                {
                    byte value = 0;
                    for (var bit = 0; bit < 8; bit++)
                    {
                        if (tdi[runStart + b * 8 + bit])
                            value |= (byte)(1 << bit);
                    }
                    commands.Add(value);
                }

                segments.Add(new Segment(SegmentKind.DataBytes, runStart, byteCount * 8));
            }

            if (bitCount > 0)
            {
                var bitStart = runStart + byteCount * 8;
                byte value = 0;
                for (var bit = 0; bit < bitCount; bit++)
                {
                    if (tdi[bitStart + bit])
                        value |= (byte)(1 << bit);
                }

                commands.Add(ShiftBitsOutIn);
                commands.Add((byte)(bitCount - 1));
                commands.Add(value);

                segments.Add(new Segment(SegmentKind.DataBits, bitStart, bitCount));
            }
        }

        commands.Add(SendImmediate);

        var expected = segments.Sum(s => s.Kind == SegmentKind.DataBytes ? s.Count / 8 : 1);

        var written = Write(commands.ToArray());
        if (written.IsFailure)
            return written;

        var response = Read(expected);
        if (response.IsFailure)
            return OperationResult.Failure(response.ErrorMessage);

        var data = response.Value!;
        var offset = 0;

        foreach (var segment in segments)
        {
            switch (segment.Kind)
            {
                case SegmentKind.TmsBit:
                    // A single TMS bit shifts in at bit 7
                    tdo[segment.Start] = (data[offset++] & 0x80) != 0;
                    break;

                case SegmentKind.DataBytes:
                    for (var b = 0; b < segment.Count / 8; b++)
                    {
                        var value = data[offset++];
                        for (var bit = 0; bit < 8; bit++)
                            tdo[segment.Start + b * 8 + bit] = (value & (1 << bit)) != 0;
                    }
                    break;

                case SegmentKind.DataBits:
                    // Bit commands shift in from the top, the received bits end up in the upper bits of the byte
                    var bits = data[offset++] >> (8 - segment.Count);
                    for (var bit = 0; bit < segment.Count; bit++)
                        tdo[segment.Start + bit] = (bits & (1 << bit)) != 0;
                    break;
            }
        }

        return OperationResult.Success();
    }

    private static void AddTmsBit(List<byte> commands, List<Segment> segments, int index, bool tms, bool tdi)
    {
        commands.Add(ShiftTmsOutIn);
        commands.Add(0x00);
        commands.Add((byte)((tdi ? 0x80 : 0x00) | (tms ? 0x01 : 0x00)));
        segments.Add(new Segment(SegmentKind.TmsBit, index, 1));
    }

    private OperationResult ConfigureMpsse(JtagProbeOptions options)
    {
        if (_resetDevice!(_handle) != FtOk ||
            _setUsbParameters!(_handle, 65536, 65536) != FtOk ||
            _setTimeouts!(_handle, ReadTimeoutMs, ReadTimeoutMs) != FtOk ||
            _setLatencyTimer!(_handle, 1) != FtOk ||
            _setBitMode!(_handle, 0, BitModeReset) != FtOk ||
            _setBitMode!(_handle, 0, BitModeMpsse) != FtOk ||
            _purge!(_handle, PurgeRxTx) != FtOk)
        {
            return OperationResult.Failure("Could not switch the FTDI device to MPSSE mode.");
        }

        Thread.Sleep(50);

        var synced = Synchronize();
        if (synced.IsFailure)
            return synced;

        var divisor = Math.Clamp((int)Math.Ceiling(BaseClockKHz / 2.0 / Math.Max(1, options.FrequencyKHz)) - 1, 0, 0xFFFF);

        // TCK, TDI and TMS are outputs, TMS idles high
        var direction = (byte)(options.FtdiLowByteDirection | 0x0B);
        var value = (byte)((options.FtdiLowByteValue & ~0x07) | 0x08);

        return Write(
        [
            DisableClockDivideBy5,
            DisableAdaptiveClocking,
            DisableThreePhaseClocking,
            LoopbackOff,
            SetClockDivisor, (byte)(divisor & 0xFF), (byte)(divisor >> 8),
            SetLowByte, value, direction
        ]);
    }

    // An invalid opcode is answered with 0xFA followed by the opcode, which proves that the MPSSE is listening
    private OperationResult Synchronize()
    {
        var written = Write([BadCommand]);
        if (written.IsFailure)
            return written;

        var response = Read(2);
        if (response.IsFailure || response.Value![0] != 0xFA || response.Value[1] != BadCommand)
            return OperationResult.Failure("The FTDI device did not respond to MPSSE commands.");

        return OperationResult.Success();
    }

    private OperationResult Write(byte[] data)
    {
        if (_write!(_handle, data, (uint)data.Length, out var written) != FtOk || written != data.Length)
            return OperationResult.Failure("Writing to the FTDI device failed.");

        return OperationResult.Success();
    }

    private OperationResult<byte[]> Read(int count)
    {
        var data = new byte[count];
        var received = 0;
        var stopwatch = Stopwatch.StartNew();

        while (received < count)
        {
            if (_getQueueStatus!(_handle, out var available) != FtOk)
                return OperationResult<byte[]>.Failure("Reading from the FTDI device failed.");

            if (available == 0)
            {
                if (stopwatch.ElapsedMilliseconds > ReadTimeoutMs)
                    return OperationResult<byte[]>.Timeout($"FTDI device returned {received} of {count} bytes.");

                Thread.Sleep(0);
                continue;
            }

            var chunk = new byte[Math.Min(available, (uint)(count - received))];
            if (_read!(_handle, chunk, (uint)chunk.Length, out var read) != FtOk)
                return OperationResult<byte[]>.Failure("Reading from the FTDI device failed.");

            Array.Copy(chunk, 0, data, received, read);
            received += (int)read;
        }

        return OperationResult<byte[]>.Success(data);
    }

    private OperationResult EnsureLoaded()
    {
        if (_openEx != null)
            return OperationResult.Success();

        // A failed search is repeated on the next attempt, so a library installed or copied meanwhile is found
        _library ??= NativeLibraryLoader.TryLoad(LibraryNames(), []);

        if (_library is not { } library)
        {
            return OperationResult.Failure(NativeLibraryLoader.NotFoundMessage(
                "The FTDI D2XX library",
                "Please install the FTDI D2XX driver.",
                LibraryNames().First()));
        }

        try
        {
            _createDeviceInfoList = NativeLibraryLoader.GetFunction<CreateDeviceInfoListDelegate>(library, "FT_CreateDeviceInfoList");
            _getDeviceInfoDetail = NativeLibraryLoader.GetFunction<GetDeviceInfoDetailDelegate>(library, "FT_GetDeviceInfoDetail");
            _close = NativeLibraryLoader.GetFunction<HandleDelegate>(library, "FT_Close");
            _resetDevice = NativeLibraryLoader.GetFunction<HandleDelegate>(library, "FT_ResetDevice");
            _purge = NativeLibraryLoader.GetFunction<PurgeDelegate>(library, "FT_Purge");
            _setUsbParameters = NativeLibraryLoader.GetFunction<SetUsbParametersDelegate>(library, "FT_SetUSBParameters");
            _setTimeouts = NativeLibraryLoader.GetFunction<SetTimeoutsDelegate>(library, "FT_SetTimeouts");
            _setLatencyTimer = NativeLibraryLoader.GetFunction<SetLatencyTimerDelegate>(library, "FT_SetLatencyTimer");
            _setBitMode = NativeLibraryLoader.GetFunction<SetBitModeDelegate>(library, "FT_SetBitMode");
            _getQueueStatus = NativeLibraryLoader.GetFunction<GetQueueStatusDelegate>(library, "FT_GetQueueStatus");
            _read = NativeLibraryLoader.GetFunction<ReadWriteDelegate>(library, "FT_Read");
            _write = NativeLibraryLoader.GetFunction<ReadWriteDelegate>(library, "FT_Write");
            _openEx = NativeLibraryLoader.GetFunction<OpenExDelegate>(library, "FT_OpenEx");
        }
        catch (EntryPointNotFoundException ex)
        {
            // Unloaded and searched again next time, so the file can be replaced without restarting
            NativeLibrary.Free(library);
            _openEx = null;
            _library = null;
            return OperationResult.Failure($"The installed FTDI driver is not supported: {ex.Message}");
        }

        return OperationResult.Success();
    }

    private static IEnumerable<string> LibraryNames()
    {
        if (OperatingSystem.IsWindows())
            return ["ftd2xx.dll", "ftd2xx64.dll"];

        if (OperatingSystem.IsMacOS())
            return ["libftd2xx.dylib", "/usr/local/lib/libftd2xx.dylib"];

        return ["libftd2xx.so", "/usr/local/lib/libftd2xx.so"];
    }

    private static string ReadCString(byte[] bytes)
    {
        var length = Array.IndexOf(bytes, (byte)0);
        return Encoding.ASCII.GetString(bytes, 0, length < 0 ? bytes.Length : length).Trim();
    }
}