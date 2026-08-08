using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        ShellCommand shellCommand,
        string? projectDirectory = null,
        CancellationToken cancellationToken = default,
        List<CustomVariable>? runtimeVariables = null)
    {
        try
        {
            var parsedCommand = CommandProcessor.CompileToString(shellCommand.Command, runtimeVariables);
            var workingDir = projectDirectory ?? AppContext.BaseDirectory;
            
            if (IsDirectLaunch(parsedCommand, out var exe, out var args))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    WorkingDirectory = workingDir,
                    UseShellExecute = false
                };

                using var process = Process.Start(psi);
                if (process is null)
                    return OperationResult<double>.Failure("Failed to start process.");

                await process.WaitForExitAsync(cancellationToken);
                return OperationResult<double>.Success(process.ExitCode);
            }
            
            var (terminalExe, argsFormat) = GetLinuxTerminal(shellCommand.Option);
            var escaped = parsedCommand.Replace("\"", "\\\"");
            var terminalArgs = string.Format(argsFormat, escaped);

            var terminalPsi = new ProcessStartInfo
            {
                FileName = terminalExe,
                Arguments = terminalArgs,
                WorkingDirectory = workingDir,
                UseShellExecute = false
            };

            using var terminalProcess = Process.Start(terminalPsi);
            if (terminalProcess is null)
                return OperationResult<double>.Failure("Failed to start terminal emulator.");

            await terminalProcess.WaitForExitAsync(cancellationToken);
            return OperationResult<double>.Success(terminalProcess.ExitCode);
        }
        catch (Exception ex)
        {
            return OperationResult<double>.Failure(ex.Message);
        }
    }

    private static bool IsDirectLaunch(string command, out string exe, out string args)
    {
        exe = command;
        args = "";

        command = command.Trim();
        
        if (command.StartsWith("\""))
        {
            var endQuote = command.IndexOf('"', 1);
            if (endQuote <= 0)
            {
                var trimmed = exe.Trim('"');
                return File.Exists(trimmed) || trimmed.Contains('/');
            }

            exe = command.Substring(1, endQuote - 1);
            args = command.Substring(endQuote + 1).Trim();
        }
        else
        {
            var parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            exe = parts[0];
            if (parts.Length > 1)
                args = parts[1];
        }
        
        return File.Exists(exe) || exe.StartsWith("./") || exe.Contains('/');
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
        var keepOpen = mode == ShellCommandOptions.KEEP_OPEN;

        if (CommandExists("ptyxis"))
            return keepOpen
                ? ("ptyxis", "-e bash -c \"{0}; exec bash\"")
                : ("ptyxis", "-e bash -c \"{0}\"");

        if (CommandExists("gnome-terminal"))
            return keepOpen
                ? ("gnome-terminal", "-- bash -c \"{0}; exec bash\"")
                : ("gnome-terminal", "-- bash -c \"{0}\"");

        if (CommandExists("konsole"))
            return keepOpen
                ? ("konsole", "-e bash -c \"{0}; exec bash\"")
                : ("konsole", "-e bash -c \"{0}\"");

        if (CommandExists("xfce4-terminal"))
            return keepOpen
                ? ("xfce4-terminal", "--hold -e \"{0}\"")
                : ("xfce4-terminal", "-e \"{0}\"");

        return keepOpen
            ? ("xterm", "-hold -e \"{0}\"")
            : ("xterm", "-e \"{0}\"");
    }
}