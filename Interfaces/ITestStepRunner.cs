using System.Threading;
using System.Threading.Tasks;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

public interface ITestStepRunner
{
    Task<bool> ExecuteAsync(TestStepViewModel step, CancellationToken token);
}

