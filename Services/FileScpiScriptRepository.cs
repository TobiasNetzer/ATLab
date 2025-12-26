using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public sealed class FileScpiScriptRepository : IScpiScriptRepository
{
    private readonly string _folder;

    public FileScpiScriptRepository(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(_folder);
    }

    private string GetPath(string id) => Path.Combine(_folder, $"{id}.json");

    public async Task<IReadOnlyList<ScpiScript>> LoadAllAsync(CancellationToken ct = default)
    {
        var result = new List<ScpiScript>();
        
        var options = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        foreach (var file in Directory.EnumerateFiles(_folder, "*.json"))
        {
            await using var stream = File.OpenRead(file);
            var script = await JsonSerializer.DeserializeAsync<ScpiScript>(stream, options, ct);
            if (script != null)
                result.Add(script);
        }

        return result.OrderBy(s => s.Name).ToList();
    }

    public async Task<ScpiScript?> LoadAsync(string id, CancellationToken ct = default)
    {
        var path = GetPath(id);
        if (!File.Exists(path))
            return null;
        
        var options = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ScpiScript>(stream, options, ct);
    }

    public async Task SaveAsync(ScpiScript script, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(script.Id))
            script.Id = Guid.NewGuid().ToString("N");
        
        var options = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        var path = GetPath(script.Id);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, script, options, ct);
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var path = GetPath(id);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }
}
