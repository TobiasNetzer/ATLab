using System;
using System.Diagnostics;
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
            var flag = mode == ShellCommandOptions.CLOSE_WHEN_DONE ? "/c" : "/k";

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"{flag} {command}",
                UseShellExecute = true,
                CreateNoWindow = false
            };

            using var process = Process.Start(psi);

            if (process is null)
                return OperationResult<double>.Failure("Failed to start cmd.exe");

            await process.WaitForExitAsync(cancellationToken);

            return OperationResult<double>.Success(process.ExitCode);
        }
        catch (Exception ex)
        {
            return OperationResult<double>.Failure(ex.Message);
        }
    }
}
