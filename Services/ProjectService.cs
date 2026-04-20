using System.IO;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ATLab.Services;

public partial class ProjectService : ObservableObject, IProjectService
{
    private readonly IFileDialogService _fileDialogService;
    private readonly IFileService _fileService;
    private readonly ISettingsService _settingsService;
    private readonly IMessageBoxService _messageBoxService;

    [ObservableProperty]
    private string? _currentFilePath;

    [ObservableProperty]
    private bool _isDirty;

    public string ProjectName =>
        string.IsNullOrEmpty(CurrentFilePath)
            ? "Untitled"
            : Path.GetFileNameWithoutExtension(CurrentFilePath);

    public ProjectService(
        IFileDialogService fileDialogService,
        IFileService fileService,
        ISettingsService settingsService,
        IMessageBoxService messageBoxService)
    {
        _fileDialogService = fileDialogService;
        _fileService = fileService;
        _settingsService = settingsService;
        _messageBoxService = messageBoxService;
    }

    public async Task<AtlabFileDto?> OpenFileAsync()
    {
        if (!await ConfirmAndContinueIfDirtyAsync())
            return null;

        var file = await _fileDialogService.OpenFileAsync("ATLab files", new[] { "atlab" });

        if (file is not null)
        {
            return await LoadAsync(file.Path.LocalPath);
        }

        return null;
    }

    public async Task<AtlabFileDto?> LoadAsync(string path)
    {
        var dto = await _fileService.LoadAsync(path);
        if (dto == null)
            return dto;
        
        CurrentFilePath = path;
        _settingsService.Settings.LastOpenedFile = path;
        IsDirty = false;
        return dto;
    }

    public async Task<bool> SaveAsync(AtlabFileDto dto)
    {
        if (string.IsNullOrWhiteSpace(CurrentFilePath))
            return await SaveAsAsync(dto);
        
        await _fileService.SaveAsync(CurrentFilePath, dto);
        IsDirty = false;
        return true;
    }

    public async Task<bool> SaveAsAsync(AtlabFileDto dto)
    {
        var file = await _fileDialogService.SaveFileAsync("ATLab files", "Test.atlab", "atlab", new[] { "atlab" });

        if (file is null)
            return false;
        
        await _fileService.SaveAsync(file.Path.LocalPath, dto);
        CurrentFilePath = file.Path.LocalPath;
        _settingsService.Settings.LastOpenedFile = file.Path.LocalPath;
        IsDirty = false;
        return true;
    }

    public async Task<bool> NewProjectAsync()
    {
        if (!await ConfirmAndContinueIfDirtyAsync())
            return false;

        CurrentFilePath = null;
        IsDirty = false;
        return true;
    }

    public async Task<bool> ConfirmAndContinueIfDirtyAsync()
    {
        if (IsDirty)
        {
            var result = await _messageBoxService.ShowConfirmationAsync(
                "Unsaved Changes", 
                "You have unsaved changes. Continue without saving?",
                "Continue",
                "Cancel");
            if (!result) return false;
        }

        IsDirty = false;
        return true;
    }

    public void UpdateLastSavedState(AtlabFileDto dto)
    {
        IsDirty = false;
    }
}