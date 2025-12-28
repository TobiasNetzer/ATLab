using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

public interface ITestStepRunner
{
    Task<OperationResult<double>> ExecuteAsync(TestStepViewModel step, CancellationToken token);
}

