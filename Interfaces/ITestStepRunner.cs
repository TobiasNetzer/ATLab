using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

/// <summary>
/// Executes one test step: switches its relays, waits for its delay and acquires the measured value from its
/// evaluation source.
/// </summary>
public interface ITestStepRunner
{
    Task<OperationResult<double>> ExecuteAsync(TestStepViewModel step, List<CustomVariable> runtimeVariables, CancellationToken token);
}

