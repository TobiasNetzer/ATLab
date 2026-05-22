using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class FileContentReader : IFileContentReader
{
    public async Task<OperationResult<byte[]>> ReadAsync(string path, CancellationToken token = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return OperationResult<byte[]>.Failure("File path is empty.");
            }

            if (!File.Exists(path))
            {
                return OperationResult<byte[]>.Failure($"File not found: {path}");
            }

            var content = await File.ReadAllBytesAsync(path, token);
            return OperationResult<byte[]>.Success(content);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OperationResult<byte[]>.Failure($"Error reading file: {ex.Message}");
        }
    }
}
