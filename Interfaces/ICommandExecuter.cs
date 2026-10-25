using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

/// <summary>
/// Sends script commands to a device from the Device Manager and returns the answer of the command marked for
/// evaluation. Keeps the device connection open between steps until it is released.
/// </summary>
public interface ICommandExecutor : IAsyncDisposable
{
    Task<OperationResult<byte[]>> ExecuteAsync(
        ScriptCommand command,
        string targetDeviceId,
        CancellationToken token,
        List<CustomVariable>? runtimeVariables = null);

    Task<OperationResult<byte[]>> ExecuteAsync(
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
}
