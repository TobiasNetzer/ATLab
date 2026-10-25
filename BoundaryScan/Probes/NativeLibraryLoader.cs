using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ATLab.BoundaryScan.Probes;

/// <summary>
/// Vendor libraries are installed with the probe drivers and not shipped with ATLab, so they are resolved at runtime.
/// A library copied into the ATLab folder takes precedence, which also covers installations in unusual places.
/// </summary>
internal static class NativeLibraryLoader
{
    public static IntPtr? TryLoad(IEnumerable<string> fileNames, IEnumerable<string> searchDirectories)
    {
        var names = fileNames.ToList();

        // Linux and macOS do not search the application folder on their own
        if (TryLoadFrom(AppContext.BaseDirectory, names) is { } local)
            return local;

        // Then the OS search path (PATH, System32, ld cache)
        foreach (var name in names)
        {
            if (NativeLibrary.TryLoad(name, out var handle))
                return handle;
        }

        foreach (var directory in searchDirectories)
        {
            if (TryLoadFrom(directory, names) is { } handle)
                return handle;
        }

        return null;
    }

    private static IntPtr? TryLoadFrom(string directory, List<string> names)
    {
        if (!Directory.Exists(directory))
            return null;

        foreach (var name in names)
        {
            var candidate = Path.Combine(directory, name);

            if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out var handle))
                return handle;
        }

        return null;
    }

    /// <summary>Error text for a missing vendor library, tells the user where a copied library is picked up.</summary>
    public static string NotFoundMessage(string product, string installHint, string fileName) =>
        $"{product} was not found. {installHint} Alternatively copy {fileName} into the ATLab folder: {AppContext.BaseDirectory}";

    /// <summary>
    /// Returns the sub directories of <paramref name="root"/> matching <paramref name="pattern"/>, newest version first.
    /// </summary>
    public static IEnumerable<string> VersionedDirectories(string root, string pattern)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            return [];

        try
        {
            return Directory.GetDirectories(root, pattern)
                .OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception)
        {
            return [];
        }
    }

    public static T GetFunction<T>(IntPtr library, string name) where T : Delegate
    {
        var address = NativeLibrary.GetExport(library, name);
        return Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    public static T? TryGetFunction<T>(IntPtr library, string name) where T : Delegate
    {
        return NativeLibrary.TryGetExport(library, name, out var address)
            ? Marshal.GetDelegateForFunctionPointer<T>(address)
            : null;
    }

    public static byte[] PackBits(bool[] bits)
    {
        var bytes = new byte[(bits.Length + 7) / 8];

        for (var i = 0; i < bits.Length; i++)
        {
            if (bits[i])
                bytes[i / 8] |= (byte)(1 << (i % 8));
        }

        return bytes;
    }

    public static bool[] UnpackBits(byte[] bytes, int count)
    {
        var bits = new bool[count];

        for (var i = 0; i < count; i++)
            bits[i] = (bytes[i / 8] & (1 << (i % 8))) != 0;

        return bits;
    }
}