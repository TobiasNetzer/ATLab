using System;
using ATLab.Interfaces;

namespace ATLab.Services;

public static class ShellCommandRunnerFactory
{
    public static IShellCommandRunner Create()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsShellCommandRunner();

        return new UnixShellCommandRunner();
    }
}
