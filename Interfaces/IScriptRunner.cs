using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface IScriptRunner
{
    Task<OperationResult> ExecuteAsync(string scriptId, string deviceId, IEnumerable<ScriptVariable> variables, CancellationToken token);
    Task<OperationResult<T>> ExecuteAsync<T>(string scriptId, string deviceId, IEnumerable<ScriptVariable> variables, CancellationToken token, ResponseMask? mask = null);
}