using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

public interface IFileContentReader
{
    Task<OperationResult<byte[]>> ReadAsync(string path, CancellationToken token = default);
}
