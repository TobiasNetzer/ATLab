using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public sealed class WindowsShellCommandRunner : IShellCommandRunner
{
    public async Task<OperationResult<double>> RunAsync(
        string command,
        ShellCommandOptions mode = ShellCommandOptions.CLOSE_WHEN_DONE,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var psi = BuildStartInfo(command, mode);

            using var process = Process.Start(psi);

            if (process is null)
                return OperationResult<double>.Failure("Failed to start process");
            
            if (psi.FileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                await process.WaitForExitAsync(cancellationToken);
                return OperationResult<double>.Success(process.ExitCode);
            }
            
            return OperationResult<double>.Success(0);
        }
        catch (Exception ex)
        {
            return OperationResult<double>.Failure(ex.Message);
        }
    }

    private static ProcessStartInfo BuildStartInfo(string command, ShellCommandOptions mode)
    {
        if (IsDirectLaunch(command, out var exe, out var args))
        {
            return new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = true,
                CreateNoWindow = true
            };
        }

        var flag = mode == ShellCommandOptions.CLOSE_WHEN_DONE ? "/c" : "/k";

        return new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"{flag} {command}",
            UseShellExecute = true,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Normal
        };
    }


    
    private static bool IsDirectLaunch(string command, out string exe, out string args)
    {
        exe = command;
        args = "";
        
        command = command.Trim();
        
        if (command.StartsWith("\""))
        {
            int endQuote = command.IndexOf('"', 1);
            if (endQuote > 0)
            {
                exe = command.Substring(1, endQuote - 1);
                args = command.Substring(endQuote + 1).Trim();
            }
        }
        else
        {
            var parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            exe = parts[0];
            if (parts.Length > 1)
                args = parts[1];
        }
        
        if (exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            return true;

        if (File.Exists(exe) || Directory.Exists(exe))
            return true;

        return false;
    }


}
