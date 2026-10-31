using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Models;

namespace ATLab.Interfaces;

/// <summary>
/// Runs a program on the test PC and returns its exit code or its evaluated output as measured value.
/// </summary>
public interface IExternalProgramRunner
{
    Task<OperationResult<double>> RunAsync(
        ExternalProgram externalProgram,
        string? projectDirectory = null,
        CancellationToken cancellationToken = default,
        List<CustomVariable>? runtimeVariables = null,
        ResponseMask? mask = null);
}
