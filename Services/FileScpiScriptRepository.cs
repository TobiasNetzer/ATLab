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
    private readonly ISettingsService _settingsService;
    private readonly IFileDialogService _fileDialogService;
    private string? _folder;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public FileScpiScriptRepository(
        ISettingsService settingsService,
        IFileDialogService fileDialogService)
    {
        _settingsService = settingsService;
        _fileDialogService = fileDialogService;
    }

    private async Task<string> GetFolderAsync()
    {
        if (!string.IsNullOrEmpty(_folder) && Directory.Exists(_folder))
        {
            return _folder;
        }

        await _semaphore.WaitAsync();
        try
        {
            if (!string.IsNullOrEmpty(_folder) && Directory.Exists(_folder))
            {
                return _folder;
            }

            var folderPath = _settingsService.Settings.ScriptRepositoryFolder;

            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                if (!string.IsNullOrWhiteSpace(folderPath))
                {
                    try
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                    catch
                    {
                        folderPath = string.Empty;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                var storageFolder = await _fileDialogService.OpenFolderAsync("Select Script Repository Folder");
                if (storageFolder != null)
                {
                    folderPath = storageFolder.Path.LocalPath;
                    _settingsService.Settings.ScriptRepositoryFolder = folderPath;
                    _settingsService.Save();
                    Directory.CreateDirectory(folderPath);
                }
                else
                {
                    folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ATLab", "Scripts");
                    _settingsService.Settings.ScriptRepositoryFolder = folderPath;
                    _settingsService.Save();
                    Directory.CreateDirectory(folderPath);
                }
            }

            _folder = folderPath;
            return _folder;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private string GetPath(string folder, string id) => Path.Combine(folder, $"{id}.json");

    public async Task<IReadOnlyList<ScpiScript>> LoadAllAsync(CancellationToken ct = default)
    {
        var folder = await GetFolderAsync();
        var result = new List<ScpiScript>();
        
        var options = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        foreach (var file in Directory.EnumerateFiles(folder, "*.json"))
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
        var folder = await GetFolderAsync();
        var path = GetPath(folder, id);
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
        var folder = await GetFolderAsync();
        if (string.IsNullOrWhiteSpace(script.Id))
            script.Id = Guid.NewGuid().ToString("N");
        
        var options = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        var path = GetPath(folder, script.Id);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, script, options, ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var folder = await GetFolderAsync();
        var path = GetPath(folder, id);
        if (File.Exists(path))
            File.Delete(path);
    }

    public async Task ConfigureRepositoryFolderAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var storageFolder = await _fileDialogService.OpenFolderAsync("Select Script Repository Folder");
            if (storageFolder != null)
            {
                var folderPath = storageFolder.Path.LocalPath;
                _settingsService.Settings.ScriptRepositoryFolder = folderPath;
                _settingsService.Save();
                Directory.CreateDirectory(folderPath);
                _folder = folderPath;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
