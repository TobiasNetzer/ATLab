using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

/// <summary>
/// Executes a script from the script folder on a device, with the step's script variables applied. Every executed
/// command is reported in <c>commandResults</c>, including the outcome of its response check.
/// </summary>
public interface IScriptRunner
{
    Task<OperationResult> ExecuteAsync(string scriptId, string deviceId, IEnumerable<CustomVariable> scriptVariables, CancellationToken token, List<CustomVariable>? runtimeVariables = null);
    Task<OperationResult<T>> ExecuteAsync<T>(string scriptId, string deviceId, IEnumerable<CustomVariable> scriptVariables, CancellationToken token,List<CustomVariable>? runtimeVariables = null, ResponseMask? mask = null, List<ScriptCommandResult>? commandResults = null);

    /// <summary>Runs the given script as it is, e.g. with unsaved changes from the Scripting tab.</summary>
    Task<OperationResult<byte[]>> ExecuteScriptAsync(CustomScript script, string deviceId, IEnumerable<CustomVariable> scriptVariables, CancellationToken token, List<CustomVariable>? runtimeVariables = null, List<ScriptCommandResult>? commandResults = null);
}