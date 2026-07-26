using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class ProjectFileService : IProjectFileService
{
    private readonly IFileDialogService _fileDialogService;
    private readonly IFileService _fileService;
    private readonly ISettingsService _settingsService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly ProjectModel _projectModel;
    
    public ProjectFileService(
        IFileDialogService fileDialogService,
        IFileService fileService,
        ISettingsService settingsService,
        IMessageBoxService messageBoxService,
        ProjectModel projectModel)
    {
        _fileDialogService = fileDialogService;
        _fileService = fileService;
        _settingsService = settingsService;
        _messageBoxService = messageBoxService;
        _projectModel = projectModel;
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
        
        _projectModel.FilePath = path;
        _settingsService.Settings.LastOpenedFile = path;
        _projectModel.IsDirty = false;
        return dto;
    }

    public async Task<bool> SaveAsync(AtlabFileDto dto)
    {
        if (string.IsNullOrWhiteSpace(_projectModel.FilePath))
            return await SaveAsAsync(dto);
        
        await _fileService.SaveAsync(_projectModel.FilePath, dto);
        _projectModel.IsDirty = false;
        return true;
    }

    public async Task<bool> SaveAsAsync(AtlabFileDto dto)
    {
        var file = await _fileDialogService.SaveFileAsync("ATLab files", "Test.atlab", "atlab", new[] { "atlab" });

        if (file is null)
            return false;
        
        await _fileService.SaveAsync(file.Path.LocalPath, dto);
        _projectModel.FilePath = file.Path.LocalPath;
        _settingsService.Settings.LastOpenedFile = file.Path.LocalPath;
        _projectModel.IsDirty = false;
        return true;
    }

    public async Task<bool> NewProjectAsync()
    {
        if (!await ConfirmAndContinueIfDirtyAsync())
            return false;

        _projectModel.FilePath = null;
        _projectModel.IsDirty = false;
        return true;
    }

    public async Task<bool> ConfirmAndContinueIfDirtyAsync()
    {
        if (_projectModel.IsDirty)
        {
            var result = await _messageBoxService.ShowConfirmationAsync(
                "Unsaved Changes", 
                "You have unsaved changes. Continue without saving?",
                "Continue",
                "Cancel");
            if (!result) return false;
        }

        _projectModel.IsDirty = false;
        return true;
    }
}