using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface ICommandExecutor
{
    Task<OperationResult<string?>> ExecuteAsync(
        ScriptCommand command,
        string deviceName,
        CancellationToken token);

    Task<OperationResult<string?>> ExecuteAsync(
        IEnumerable<ScriptCommand> commands,
        string deviceName,
        CancellationToken token);

    Task<OperationResult<T>> ExecuteAsync<T>(
        ScriptCommand command,
        string deviceName,
        CancellationToken token,
        ResponseMask? mask = null);
}
