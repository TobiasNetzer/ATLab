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

public sealed class WindowsShellCommandRunner : IShellCommandRunner
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

            var psi = BuildStartInfo(parsedCommand, shellCommand.Option, workingDir, shellCommand.IsDirectLaunch);

            using var process = Process.Start(psi);

            if (process is null)
                return OperationResult<double>.Failure("Failed to start process");

            if (!psi.FileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return OperationResult<double>.Success(0);
            
            await process.WaitForExitAsync(cancellationToken);
            return OperationResult<double>.Success(process.ExitCode);
        }
        catch (Exception ex)
        {
            return OperationResult<double>.Failure(ex.Message);
        }
    }

    private static ProcessStartInfo BuildStartInfo(string command, ShellCommandOptions mode, string projectDirectory, bool isDirectLaunch = false)
    {
        if (isDirectLaunch)
        {
            ParseExeAndArgs(command, out var exe, out var args);
            return new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = projectDirectory,
                UseShellExecute = true,
                CreateNoWindow = true
            };
        }

        var flag = mode == ShellCommandOptions.CLOSE_WHEN_DONE ? "/c" : "/k";

        return new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"{flag} {command}",
            WorkingDirectory = projectDirectory,
            UseShellExecute = true,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Normal
        };
    }
    
    private static void ParseExeAndArgs(string command, out string exe, out string args)
    {
        command = command.Trim();

        if (command.StartsWith("\""))
        {
            var endQuote = command.IndexOf('"', 1);
            if (endQuote < 0)
            {
                exe = command.Trim('"');
                args = "";
                return;
            }

            exe = command.Substring(1, endQuote - 1);
            args = command.Substring(endQuote + 1).Trim();
        }
        else
        {
            var parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            exe = parts[0];
            args = parts.Length > 1 ? parts[1] : "";
        }
    }
}