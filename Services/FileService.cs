using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class FileService : IFileService
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public string Serialize(AtlabFileDto dto)
    {
        return JsonSerializer.Serialize(dto, _options);
    }

    public AtlabFileDto? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<AtlabFileDto>(json);
    }

    public async Task SaveAsync(string path, AtlabFileDto dto)
    {
        await _semaphore.WaitAsync();
        try
        {
            var json = Serialize(dto);
            await File.WriteAllTextAsync(path, json);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<AtlabFileDto?> LoadAsync(string path)
    {
        await _semaphore.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(path);
            return Deserialize(json);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
