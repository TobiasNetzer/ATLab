using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

/// <summary>Reads and writes project files as JSON.</summary>
public class ProjectFileService : IProjectFileService
{
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILoggingService _loggingService;

    public ProjectFileService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    private string Serialize(AtlabFileDto dto)
    {
        return JsonSerializer.Serialize(dto, _options);
    }

    private AtlabFileDto? Deserialize(string json)
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

            _loggingService.Info(
                $"Project '{Path.GetFileName(path)}' saved.");
        }
        catch (Exception ex)
        {
            _loggingService.Error(
                $"Failed to save project '{Path.GetFileName(path)}'. {ex.Message}");

            throw;
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

            var project = Deserialize(json);

            if (project != null && project.FormatVersion != AtlabFileDto.CurrentFormatVersion)
            {
                throw new NotSupportedException(
                    $"Unsupported project file format version {project.FormatVersion} (expected {AtlabFileDto.CurrentFormatVersion}).");
            }

            //_loggingService.Info($"Project '{Path.GetFileName(path)}' loaded.");

            return project;
        }
        catch (Exception ex)
        {
            // Logged here only, callers treat null as "nothing loaded"
            _loggingService.Error(
                $"Failed to load project '{Path.GetFileName(path)}'. {ex.Message}");

            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}