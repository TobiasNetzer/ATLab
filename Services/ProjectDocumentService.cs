using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class ProjectDocumentService : IProjectDocumentService
{
    private readonly IFileDialogService _fileDialogService;
    private readonly IProjectStorage _projectStorage;
    private readonly ISettingsService _settingsService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly ProjectModel _projectModel;
    
    public ProjectDocumentService(
        IFileDialogService fileDialogService,
        IProjectStorage projectStorage,
        ISettingsService settingsService,
        IMessageBoxService messageBoxService,
        ProjectModel projectModel)
    {
        _fileDialogService = fileDialogService;
        _projectStorage = projectStorage;
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
            return await OpenAsync(file.Path.LocalPath);
        }

        return null;
    }

    public async Task<AtlabFileDto?> OpenAsync(string path)
    {
        var dto = await _projectStorage.LoadAsync(path);
        if (dto == null)
            return dto;
        
        _projectModel.MarkSaved(path);
        _settingsService.Settings.LastOpenedFile = path;
        return dto;
    }

    public async Task<bool> SaveAsync(AtlabFileDto dto)
    {
        if (string.IsNullOrWhiteSpace(_projectModel.FilePath))
            return await SaveAsAsync(dto);
        
        await _projectStorage.SaveAsync(_projectModel.FilePath, dto);
        _projectModel.MarkSaved();
        _settingsService.Settings.LastOpenedFile = _projectModel.FilePath;
        return true;
    }

    public async Task<bool> SaveAsAsync(AtlabFileDto dto)
    {
        var file = await _fileDialogService.SaveFileAsync("ATLab files", "Test.atlab", "atlab", new[] { "atlab" });

        if (file is null)
            return false;
        
        await _projectStorage.SaveAsync(file.Path.LocalPath, dto);
        _projectModel.MarkSaved(file.Path.LocalPath);
        _settingsService.Settings.LastOpenedFile = file.Path.LocalPath;
        return true;
    }

    public async Task<bool> NewProjectAsync()
    {
        if (!await ConfirmAndContinueIfDirtyAsync())
            return false;

        _projectModel.Reset();
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
        _projectModel.MarkSaved();
        return true;
    }
}