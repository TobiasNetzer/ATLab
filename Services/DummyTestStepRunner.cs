using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.ViewModels;

namespace ATLab.Services;

public class DummyTestStepRunner : ITestStepRunner
{
    public async Task<bool> ExecuteAsync(TestStepViewModel step, CancellationToken token)
    {
        await Task.Delay(500, token);
        return await Task.FromResult(true);
    }
}
