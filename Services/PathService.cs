using System;
using System.IO;

namespace ATLab.Services;

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

        return full.StartsWith(_root, _cmp)
            ? Path.GetRelativePath(_root, full)
            : full;
    }

    public string ToAbsolute(string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return storedPath;

        return !Path.IsPathRooted(storedPath)
            ? Path.GetFullPath(Path.Combine(_root, storedPath))
            : storedPath;
    }

    public string Normalize(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? path
            : Path.GetFullPath(path);
    }
}