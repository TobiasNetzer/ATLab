using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class DummyTestStepRunner : ITestStepRunner
{
    public async Task<TestStepResult> ExecuteAsync(TestStepViewModel step, CancellationToken token)
    {
        await Task.Delay(500, token);
        return new TestStepResult(true, 0.0);
    }
}
