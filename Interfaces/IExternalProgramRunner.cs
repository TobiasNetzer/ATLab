using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface IExternalProgramRunner
{
    Task<OperationResult<double>> RunAsync(
        ExternalProgram externalProgram,
        string? projectDirectory = null,
        CancellationToken cancellationToken = default,
        List<CustomVariable>? runtimeVariables = null,
        ResponseMask? mask = null);
}
