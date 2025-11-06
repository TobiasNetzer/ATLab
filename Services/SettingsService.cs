using System;
using System.IO;
using System.Text.Json;
using ATLab.Models;

namespace ATLab.Services;

public class SettingsService
{
    private readonly string _filePath;
    public AppSettings Settings { get; private set; }

    public SettingsService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ATLab"
        );

        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");

        Settings = Load();
    }

    private AppSettings Load()
    {
        if (File.Exists(_filePath))
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_filePath))
                    ?? new AppSettings();

        return new AppSettings();
    }

    public void Save()
    {
        File.WriteAllText(_filePath, 
            JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}

