using System.Threading;
using System.Threading.Tasks;
using ATLab.Models;

namespace ATLab.Interfaces;

/// <summary>Reads the content of a file for the File evaluation source.</summary>
public interface IFileContentReader
{
    Task<OperationResult<byte[]>> ReadAsync(string path, CancellationToken token = default);
}
