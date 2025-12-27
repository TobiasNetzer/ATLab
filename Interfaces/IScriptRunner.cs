using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface IScriptRunner
{
    Task ExecuteAsync(string scriptId, string deviceName, IEnumerable<ScpiVariable> variables, CancellationToken token);
    Task<T?> ExecuteAsync<T>(string scriptId, string deviceName, IEnumerable<ScpiVariable> variables, CancellationToken token);
}