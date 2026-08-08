using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface IShellCommandRunner
{
    Task<OperationResult<double>> RunAsync(
        ShellCommand shellCommand,
        string? projectDirectory = null,
        CancellationToken cancellationToken = default,
        List<CustomVariable>? runtimeVariables = null);
}
