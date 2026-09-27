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

public sealed class FileScriptRepository : IScriptRepository
{
    public event EventHandler<string?>? RepositoryFolderChanged;

    private readonly IFileDialogService _fileDialogService;
    private readonly ILoggingService _loggingService;

    private string? _explicitFolder;
    private string? _folder;

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, CustomScript> _cache = new();

    public FileScriptRepository(
        IFileDialogService fileDialogService,
        ILoggingService loggingService)
    {
        _fileDialogService = fileDialogService;
        _loggingService = loggingService;
    }

    public void SetRepositoryFolder(string? folderPath)
    {
        _explicitFolder = folderPath;
        _folder = null;

        lock (_cache)
        {
            _cache.Clear();
        }

        RepositoryFolderChanged?.Invoke(this, folderPath);
    }

    private async Task<string> GetFolderAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            if (!string.IsNullOrEmpty(_folder) && Directory.Exists(_folder))
            {
                return _folder;
            }

            var folderPath = _explicitFolder;

            if (!string.IsNullOrWhiteSpace(folderPath) &&
                !Directory.Exists(folderPath))
            {
                try
                {
                    Directory.CreateDirectory(folderPath);

                    _loggingService.Info(
                        $"Created repository folder '{folderPath}'.");
                }
                catch (Exception ex)
                {
                    _loggingService.Error(
                        $"Failed to create repository folder '{folderPath}'. {ex.Message}");

                    folderPath = string.Empty;
                }
            }

            if (string.IsNullOrWhiteSpace(folderPath) ||
                !Directory.Exists(folderPath))
            {
                folderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ATLab",
                    "Scripts");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);

                    _loggingService.Info(
                        $"Created default repository folder '{folderPath}'.");
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

    private static JsonSerializerOptions CreateJsonOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private string GetPath(string folder, string id)
        => Path.Combine(folder, $"{id}.json");

    public async Task<IReadOnlyList<CustomScript>> LoadAllAsync(
        CancellationToken ct = default)
    {
        var folder = await GetFolderAsync();

        var result = new List<CustomScript>();

        foreach (var file in Directory.EnumerateFiles(folder, "*.json"))
        {
            try
            {
                await using var stream = File.OpenRead(file);

                var script = await JsonSerializer.DeserializeAsync<CustomScript>(
                    stream,
                    CreateJsonOptions(),
                    ct);

                if (script != null)
                {
                    result.Add(script);
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(
                    $"Failed to load script '{Path.GetFileName(file)}'. {ex.Message}");
            }
        }

        var ordered = result
            .OrderBy(s => s.Name)
            .ToList();

        lock (_cache)
        {
            _cache.Clear();

            foreach (var script in ordered)
            {
                _cache[script.Id] = script;
            }
        }

        return ordered;
    }

    public async Task<CustomScript?> LoadAsync(
        string id,
        CancellationToken ct = default)
    {
        lock (_cache)
        {
            if (_cache.TryGetValue(id, out var cached))
            {
                return cached;
            }
        }

        var folder = await GetFolderAsync();
        var path = GetPath(folder, id);

        if (!File.Exists(path))
        {
            _loggingService.Warning(
                $"Script '{id}' does not exist.");

            return null;
        }

        try
        {
            await using var stream = File.OpenRead(path);

            var script = await JsonSerializer.DeserializeAsync<CustomScript>(
                stream,
                CreateJsonOptions(),
                ct);

            if (script != null)
            {
                lock (_cache)
                {
                    _cache[id] = script;
                }

                _loggingService.Info(
                    $"Loaded script '{script.Name}' ({script.Id}).");
            }

            return script;
        }
        catch (Exception ex)
        {
            _loggingService.Error(
                $"Failed to load script '{id}'. {ex.Message}");

            return null;
        }
    }

    public async Task SaveAsync(
        CustomScript script,
        CancellationToken ct = default)
    {
        try
        {
            var folder = await GetFolderAsync();

            if (string.IsNullOrWhiteSpace(script.Id))
            {
                script.Id = Guid.NewGuid().ToString("N");

                _loggingService.Info(
                    $"Generated new ID for script '{script.Name}'.");
            }

            var path = GetPath(folder, script.Id);

            await using var stream = File.Create(path);

            await JsonSerializer.SerializeAsync(
                stream,
                script,
                CreateJsonOptions(),
                ct);

            lock (_cache)
            {
                _cache[script.Id] = script;
            }

            _loggingService.Info(
                $"Saved script '{script.Name}'.");
        }
        catch (Exception ex)
        {
            _loggingService.Error(
                $"Failed to save script '{script.Name}'. {ex.Message}");

            throw;
        }
    }

    public async Task DeleteAsync(
        string id,
        string name,
        CancellationToken ct = default)
    {
        var folder = await GetFolderAsync();
        var path = GetPath(folder, id);

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);

                _loggingService.Info(
                    $"Script '{name}' deleted successfully.");
            }
            else
            {
                _loggingService.Warning(
                    $"Attempted to delete missing script '{name}' '{id}'.");
            }

            lock (_cache)
            {
                _cache.Remove(id);
            }
        }
        catch (Exception ex)
        {
            _loggingService.Error(
                $"Failed to delete script '{name}' '{id}'. {ex.Message}");

            throw;
        }
    }

    public async Task ConfigureRepositoryFolderAsync()
    {
        await _semaphore.WaitAsync();

        try
        {
            var storageFolder =
                await _fileDialogService.OpenFolderAsync(
                    "Select Script Repository Folder");

            if (storageFolder != null)
            {
                var folderPath = storageFolder.Path.LocalPath;

                _explicitFolder = folderPath;
                _folder = folderPath;

                lock (_cache)
                {
                    _cache.Clear();
                }

                _loggingService.Info(
                    $"Repository folder set to '{folderPath}'.");

                RepositoryFolderChanged?.Invoke(this, folderPath);
            }
            else
            {
                _loggingService.Info(
                    "Repository folder selection cancelled.");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}