using System;
using System.IO;

namespace ATLab.Services;

/// <summary>
/// Converts paths between absolute and relative to the project folder. Relative paths use '/' so projects work on
/// Windows and Linux.
/// </summary>
public class PathService
{
    private readonly string _root;
    private readonly StringComparison _cmp;

    public PathService(string projectFilePath)
    {
        if (string.IsNullOrWhiteSpace(projectFilePath))
            throw new ArgumentException("Project file path cannot be null or empty.", nameof(projectFilePath));

        var projectDir = Path.GetDirectoryName(projectFilePath)
                         ?? throw new InvalidOperationException("Project file has no directory.");

        _root = Path.GetFullPath(projectDir);

        _cmp = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }

    public string ToRelative(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return filePath;

        var full = Path.GetFullPath(filePath);

        // Store relative paths with '/' so project files stay portable between Windows and Linux
        return IsUnderRoot(full)
            ? Path.GetRelativePath(_root, full).Replace('\\', '/')
            : full;
    }

    public string ToAbsolute(string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return storedPath;

        if (Path.IsPathRooted(storedPath))
            return storedPath;
        
        var portable = storedPath.Replace('\\', '/');
        return Path.GetFullPath(Path.Combine(_root, portable));
    }

    private bool IsUnderRoot(string fullPath)
    {
        if (!fullPath.StartsWith(_root, _cmp))
            return false;
        
        return fullPath.Length == _root.Length
               || Path.EndsInDirectorySeparator(_root)
               || fullPath[_root.Length] == Path.DirectorySeparatorChar
               || fullPath[_root.Length] == Path.AltDirectorySeparatorChar;
    }

    public string Normalize(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? path
            : Path.GetFullPath(path);
    }
}