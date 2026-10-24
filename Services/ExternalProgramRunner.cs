using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Helpers;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public sealed class ExternalProgramRunner : IExternalProgramRunner
{
    public async Task<OperationResult<double>> RunAsync(
        ExternalProgram externalProgram,
        string? projectDirectory = null,
        CancellationToken cancellationToken = default,
        List<CustomVariable>? runtimeVariables = null,
        ResponseMask? mask = null)
    {
        try
        {
            var parsedCommand = CommandProcessor.CompileToString(externalProgram.Command, runtimeVariables);
            ParseExeAndArgs(parsedCommand, out var exe, out var args);

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = projectDirectory ?? AppContext.BaseDirectory
            };

            if (externalProgram.EvaluateOutput)
                return await RunAndEvaluateOutputAsync(psi, mask, cancellationToken);

            // On Windows, ShellExecute gives console programs their own window and resolves registered App Paths
            psi.UseShellExecute = OperatingSystem.IsWindows();

            using var process = Process.Start(psi);
            if (process is null)
                return OperationResult<double>.Failure("Failed to start process.");

            await WaitForExitAsync(process, cancellationToken);
            return OperationResult<double>.Success(process.ExitCode);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OperationResult<double>.Failure(ex.Message);
        }
    }
    
    private static async Task<OperationResult<double>> RunAndEvaluateOutputAsync(
        ProcessStartInfo psi,
        ResponseMask? mask,
        CancellationToken cancellationToken)
    {
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;

        using var process = Process.Start(psi);
        if (process is null)
            return OperationResult<double>.Failure("Failed to start process.");

        // Read raw bytes so binary response masks work on the output as well
        using var stdout = new MemoryStream();
        var stdoutTask = process.StandardOutput.BaseStream.CopyToAsync(stdout, cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await WaitForExitAsync(process, cancellationToken);
        await stdoutTask;
        var stderr = (await stderrTask).Trim();

        if (process.ExitCode != 0)
            return OperationResult<double>.Failure(string.IsNullOrEmpty(stderr)
                ? $"Process exited with code {process.ExitCode}."
                : $"Process exited with code {process.ExitCode}: {stderr}");

        var processed = ResponseProcessor.Process(stdout.ToArray(), mask).Trim();

        return NumberHelper.TryParseDecimal(processed, out var result)
            ? OperationResult<double>.Success(result)
            : OperationResult<double>.Failure($"Failed to parse process output to double: {processed}");
    }
    
    private static async Task WaitForExitAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // process already gone or not accessible
            }

            throw;
        }
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