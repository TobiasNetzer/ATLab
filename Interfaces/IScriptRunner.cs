using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

/// <summary>
/// Executes a script from the script folder on a device, with the step's script variables applied.
/// </summary>
public interface IScriptRunner
{
    Task<OperationResult> ExecuteAsync(string scriptId, string deviceId, IEnumerable<CustomVariable> scriptVariables, CancellationToken token, List<CustomVariable>? runtimeVariables = null);
    Task<OperationResult<T>> ExecuteAsync<T>(string scriptId, string deviceId, IEnumerable<CustomVariable> scriptVariables, CancellationToken token,List<CustomVariable>? runtimeVariables = null, ResponseMask? mask = null, List<string>? checkFailures = null);
}