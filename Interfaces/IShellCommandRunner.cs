using System.Threading;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface IShellCommandRunner
{
    Task<OperationResult<double>> RunAsync(
        string command,
        ShellCommandOptions mode = ShellCommandOptions.CLOSE_WHEN_DONE,
        CancellationToken cancellationToken = default);
}
