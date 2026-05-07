using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Helpers;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public sealed class UnixShellCommandRunner : IShellCommandRunner
{
    public async Task<OperationResult<double>> RunAsync(
        string command,
        ShellCommandOptions mode = ShellCommandOptions.CLOSE_WHEN_DONE,
        CancellationToken cancellationToken = default,
        List<CustomVariable>? runtimeVariables = null)
    {
        try
        {
            var parsedCommand = VariableResolver.Resolve(command, runtimeVariables);
            var (exe, argsFormat) = GetLinuxTerminal(mode);
            var args = string.Format(argsFormat, parsedCommand);

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);

            if (process is null)
                return OperationResult<double>.Failure("Failed to start terminal emulator.");

            await process.WaitForExitAsync(cancellationToken);

            return OperationResult<double>.Success(process.ExitCode);
        }
        catch (Exception ex)
        {
            return OperationResult<double>.Failure(ex.Message);
        }
    }
    
    private static bool CommandExists(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "which",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            process!.WaitForExit();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
    
    private static (string exe, string argsFormat) GetLinuxTerminal(ShellCommandOptions mode)
    {
        bool keepOpen = mode == ShellCommandOptions.KEEP_OPEN;

        if (CommandExists("ptyxis"))
        {
            return keepOpen
                ? ("ptyxis", "-e bash -c \"{0}; exec bash\"")
                : ("ptyxis", "-e bash -c \"{0}\"");
        }

        if (CommandExists("gnome-terminal"))
        {
            return keepOpen
                ? ("gnome-terminal", "-- bash -c \"{0}; exec bash\"")
                : ("gnome-terminal", "-- bash -c \"{0}\"");
        }

        if (CommandExists("konsole"))
        {
            return keepOpen
                ? ("konsole", "-e bash -c \"{0}; exec bash\"")
                : ("konsole", "-e bash -c \"{0}\"");
        }

        if (CommandExists("xfce4-terminal"))
        {
            return keepOpen
                ? ("xfce4-terminal", "--hold -e \"{0}\"")
                : ("xfce4-terminal", "-e \"{0}\"");
        }

        // fallback
        return keepOpen
            ? ("xterm", "-hold -e {0}")
            : ("xterm", "-e {0}");
    }
}
