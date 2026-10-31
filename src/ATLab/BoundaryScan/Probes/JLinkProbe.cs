using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ATLab.Models;

namespace ATLab.BoundaryScan.Probes;

/// <summary>
/// SEGGER J-Link through the JLinkARM DLL of the J-Link Software Pack. Only the raw JTAG API is used,
/// the DLL never connects to a CPU.
/// </summary>
public sealed class JLinkProbe : IJtagProbe
{
    private const int HostInterfaceUsb = 1;
    private const int TargetInterfaceJtag = 0;
    private const int MaxListedProbes = 32;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr OpenDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CloseDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SelectByUsbSerialDelegate(uint serialNumber);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GetEmuListDelegate(int hostInterfaces, IntPtr connectInfos, int maxInfos);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SelectTargetInterfaceDelegate(int targetInterface);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SetSpeedDelegate(uint speedKHz);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void StoreGetRawDelegate(byte[] tdi, byte[] tdo, byte[] tms, uint numBits);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SyncBitsDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate byte HasErrorDelegate();

    // Mirrors JLINKARM_EMU_CONNECT_INFO (264 bytes)
    [StructLayout(LayoutKind.Sequential)]
    private struct EmuConnectInfo
    {
        public uint SerialNumber;
        public byte Connection;
        public uint UsbAddress;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] IpAddress;
        public int Time;
        public ulong TimeMicroseconds;
        public uint HardwareVersion;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[] MacAddress;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] Product;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] NickName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 112)]
        public byte[] FirmwareString;
        public byte IsDhcpAssignedIp;
        public byte IsDhcpAssignedIpIsValid;
        public byte NumIpConnections;
        public byte NumIpConnectionsIsValid;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 34)]
        public byte[] Padding;
    }

    private static IntPtr? _library;

    private OpenDelegate? _open;
    private CloseDelegate? _close;
    private SelectByUsbSerialDelegate? _selectByUsbSerial;
    private GetEmuListDelegate? _getEmuList;
    private SelectTargetInterfaceDelegate? _selectTargetInterface;
    private SetSpeedDelegate? _setSpeed;
    private StoreGetRawDelegate? _storeGetRaw;
    private SyncBitsDelegate? _syncBits;
    private HasErrorDelegate? _hasError;

    public bool IsOpen { get; private set; }

    public OperationResult<IReadOnlyList<JtagProbeInfo>> ListProbes()
    {
        var loaded = EnsureLoaded();
        if (loaded.IsFailure)
            return OperationResult<IReadOnlyList<JtagProbeInfo>>.Failure(loaded.ErrorMessage);

        var size = Marshal.SizeOf<EmuConnectInfo>();
        var buffer = Marshal.AllocHGlobal(size * MaxListedProbes);

        try
        {
            var count = _getEmuList!(HostInterfaceUsb, buffer, MaxListedProbes);
            if (count < 0)
                return OperationResult<IReadOnlyList<JtagProbeInfo>>.Failure($"J-Link enumeration failed ({count}).");

            var probes = new List<JtagProbeInfo>();

            for (var i = 0; i < Math.Min(count, MaxListedProbes); i++)
            {
                var info = Marshal.PtrToStructure<EmuConnectInfo>(buffer + i * size);
                var product = ReadCString(info.Product);
                probes.Add(new JtagProbeInfo(
                    info.SerialNumber.ToString(),
                    string.IsNullOrWhiteSpace(product) ? "J-Link" : product));
            }

            return OperationResult<IReadOnlyList<JtagProbeInfo>>.Success(probes);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public OperationResult Open(string? serialNumber, JtagProbeOptions options)
    {
        if (IsOpen)
            Close();

        var loaded = EnsureLoaded();
        if (loaded.IsFailure)
            return loaded;

        if (!string.IsNullOrWhiteSpace(serialNumber))
        {
            if (!uint.TryParse(serialNumber.Trim(), out var serial))
                return OperationResult.Failure($"'{serialNumber}' is not a valid J-Link serial number.");

            if (_selectByUsbSerial!(serial) < 0)
                return OperationResult.Failure($"J-Link with serial number {serial} not found.");
        }

        var error = _open!();
        if (error != IntPtr.Zero)
            return OperationResult.Failure($"Could not open J-Link: {Marshal.PtrToStringAnsi(error)}");

        _selectTargetInterface!(TargetInterfaceJtag);
        _setSpeed!((uint)Math.Max(1, options.FrequencyKHz));

        IsOpen = true;
        return OperationResult.Success();
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        _close?.Invoke();
        IsOpen = false;
    }

    public OperationResult<bool[]> Shift(bool[] tms, bool[] tdi)
    {
        if (!IsOpen)
            return OperationResult<bool[]>.Failure("J-Link is not open.");

        if (tms.Length != tdi.Length)
            throw new ArgumentException("TMS and TDI must have the same length.");

        if (tms.Length == 0)
            return OperationResult<bool[]>.Success([]);

        var tdo = new byte[(tms.Length + 7) / 8];

        _storeGetRaw!(NativeLibraryLoader.PackBits(tdi), tdo, NativeLibraryLoader.PackBits(tms), (uint)tms.Length);
        _syncBits?.Invoke();

        if (_hasError != null && _hasError() != 0)
            return OperationResult<bool[]>.Failure("J-Link reported an error during the JTAG transfer.");

        return OperationResult<bool[]>.Success(NativeLibraryLoader.UnpackBits(tdo, tms.Length));
    }

    public void Dispose() => Close();

    private OperationResult EnsureLoaded()
    {
        if (_open != null)
            return OperationResult.Success();

        // A failed search is repeated on the next attempt, so a library installed or copied meanwhile is found
        _library ??= NativeLibraryLoader.TryLoad(LibraryNames(), SearchDirectories());

        if (_library is not { } library)
        {
            return OperationResult.Failure(NativeLibraryLoader.NotFoundMessage(
                "The SEGGER J-Link library",
                "Please install the J-Link Software and Documentation Pack.",
                LibraryNames().First()));
        }

        try
        {
            _open = NativeLibraryLoader.GetFunction<OpenDelegate>(library, "JLINKARM_Open");
            _close = NativeLibraryLoader.GetFunction<CloseDelegate>(library, "JLINKARM_Close");
            _selectByUsbSerial = NativeLibraryLoader.GetFunction<SelectByUsbSerialDelegate>(library, "JLINKARM_EMU_SelectByUSBSN");
            _getEmuList = NativeLibraryLoader.GetFunction<GetEmuListDelegate>(library, "JLINKARM_EMU_GetList");
            _selectTargetInterface = NativeLibraryLoader.GetFunction<SelectTargetInterfaceDelegate>(library, "JLINKARM_TIF_Select");
            _setSpeed = NativeLibraryLoader.GetFunction<SetSpeedDelegate>(library, "JLINKARM_SetSpeed");
            _storeGetRaw = NativeLibraryLoader.GetFunction<StoreGetRawDelegate>(library, "JLINKARM_JTAG_StoreGetRaw");
            _syncBits = NativeLibraryLoader.TryGetFunction<SyncBitsDelegate>(library, "JLINKARM_JTAG_SyncBits");
            _hasError = NativeLibraryLoader.TryGetFunction<HasErrorDelegate>(library, "JLINKARM_HasError");
        }
        catch (EntryPointNotFoundException ex)
        {
            // Unloaded and searched again next time, so the file can be replaced without restarting
            NativeLibrary.Free(library);
            _open = null;
            _library = null;
            return OperationResult.Failure($"The installed J-Link DLL is not supported: {ex.Message}");
        }

        return OperationResult.Success();
    }

    private static IEnumerable<string> LibraryNames()
    {
        if (OperatingSystem.IsWindows())
            return Environment.Is64BitProcess ? ["JLink_x64.dll"] : ["JLinkARM.dll"];

        if (OperatingSystem.IsMacOS())
            return ["libjlinkarm.dylib"];

        return ["libjlinkarm.so", "libjlinkarm.so.8", "libjlinkarm.so.7"];
    }

    private static IEnumerable<string> SearchDirectories()
    {
        if (OperatingSystem.IsWindows())
        {
            var roots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            return roots
                .Select(root => Path.Combine(root, "SEGGER"))
                .SelectMany(segger => NativeLibraryLoader.VersionedDirectories(segger, "JLink*"));
        }

        if (OperatingSystem.IsMacOS())
            return NativeLibraryLoader.VersionedDirectories("/Applications/SEGGER", "JLink*");

        return NativeLibraryLoader.VersionedDirectories("/opt/SEGGER", "JLink*");
    }

    private static string ReadCString(byte[]? bytes)
    {
        if (bytes == null)
            return string.Empty;

        var length = Array.IndexOf(bytes, (byte)0);
        return Encoding.ASCII.GetString(bytes, 0, length < 0 ? bytes.Length : length).Trim();
    }
}