using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface ICommandExecutor
{
    Task<OperationResult<string?>> ExecuteAsync(
        ScriptCommand command,
        string targetDeviceId,
        CancellationToken token,
        List<CustomVariable>? runtimeVariables = null);

    Task<OperationResult<string?>> ExecuteAsync(
        IEnumerable<ScriptCommand> commands,
        string targetDeviceId,
        CancellationToken token,
        List<CustomVariable>? runtimeVariables = null);

    Task<OperationResult<T>> ExecuteAsync<T>(
        ScriptCommand command,
        string targetDeviceId,
        CancellationToken token,
        List<CustomVariable>? runtimeVariables = null,
        ResponseMask? mask = null);

    Task ReleaseDeviceAsync();
    
    void Dispose();
}
