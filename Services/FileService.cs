using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class FileService : IFileService
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

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
        var json = Serialize(dto);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<AtlabFileDto?> LoadAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        return Deserialize(json);
    }
}
