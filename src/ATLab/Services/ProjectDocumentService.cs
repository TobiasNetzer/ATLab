using System.IO;
using System.Threading.Tasks;
using ATLab.Enums;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

/// <summary>
/// File dialogs, unsaved-changes confirmation and the remembered last project around the project file.
/// </summary>
public class ProjectDocumentService : IProjectDocumentService
{
    private readonly IFileDialogService _fileDialogService;
    private readonly IProjectFileService _projectFileService;
    private readonly ISettingsService _settingsService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IHardwareAccessor _hardwareAccessor;
    private readonly ApplicationState _applicationState;
    private readonly ILoggingService _loggingService;
    private readonly ProjectModel _projectModel;
    
    public ProjectDocumentService(
        IFileDialogService fileDialogService,
        IProjectFileService projectFileService,
        ISettingsService settingsService,
        IMessageBoxService messageBoxService,
        IHardwareAccessor hardwareAccessor,
        ApplicationState applicationState,
        ILoggingService loggingService,
        ProjectModel projectModel)
    {
        _fileDialogService = fileDialogService;
        _projectFileService = projectFileService;
        _settingsService = settingsService;
        _messageBoxService = messageBoxService;
        _hardwareAccessor = hardwareAccessor;
        _applicationState = applicationState;
        _loggingService = loggingService;
        _projectModel = projectModel;
    }

    public async Task<AtlabFileDto?> OpenFileAsync()
    {
        var path = await PickProjectFileAsync();
        return path is null ? null : await OpenAsync(path);
    }

    public async Task<string?> PickProjectFileAsync()
    {
        var file = await _fileDialogService.OpenFileAsync("ATLab files", new[] { "atlab" });
        return file?.Path.LocalPath;
    }

    public async Task<AtlabFileDto?> OpenAsync(string path)
    {
        var dto = await _projectFileService.LoadAsync(path);
        if (dto == null)
            return dto;

        // Checked before the file becomes the current project, so a refused project cannot be overwritten by Save
        if (!await IsForActiveTestHardwareAsync(dto, path))
            return null;
        
        _projectModel.MarkSaved(path);
        _settingsService.Settings.LastOpenedFile = path;
        return dto;
    }

    /// <summary>
    /// A project only opens on the test hardware it was developed for, compared by the device ID the hardware
    /// reports. The simulator is checked the same way: it reports the ID of the hardware it simulates.
    /// </summary>
    private async Task<bool> IsForActiveTestHardwareAsync(AtlabFileDto dto, string path)
    {
        var targetId = TestHardwareIdFormat.Parse(dto.TargetTestHardwareId);
        var active = _hardwareAccessor.Hardware.HardwareInfo;

        if (targetId == active.DeviceId)
            return true;

        var localization = LocalizationService.Instance;
        var message = string.Format(
            localization[_applicationState.IsSimulationMode ? "Text_WrongSimulatedTestHardware" : "Text_WrongTestHardware"],
            Path.GetFileName(path),
            TestHardwareIdFormat.Format(targetId),
            TestHardwareIdFormat.GetDisplayName(active.DeviceName, active.DeviceId));

        _loggingService.Error(message);
        await _messageBoxService.ShowMessageAsync(localization["Title_WrongTestHardware"], message);
        return false;
    }

    public async Task<bool> SaveAsync(AtlabFileDto dto)
    {
        if (string.IsNullOrWhiteSpace(_projectModel.FilePath))
            return await SaveAsAsync(dto);
        
        await _projectFileService.SaveAsync(_projectModel.FilePath, dto);
        _projectModel.MarkSaved();
        _settingsService.Settings.LastOpenedFile = _projectModel.FilePath;
        return true;
    }

    public async Task<bool> SaveAsAsync(AtlabFileDto dto)
    {
        var file = await _fileDialogService.SaveFileAsync("ATLab files", "Untitled.atlab", "atlab", new[] { "atlab" });

        if (file is null)
            return false;
        
        await _projectFileService.SaveAsync(file.Path.LocalPath, dto);
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

    /// <summary>
    /// Asks about unsaved changes, then empties the project. It is no longer the last opened file, so the next start
    /// shows the welcome screen.
    /// </summary>
    public async Task<bool> CloseProjectAsync()
    {
        if (!await ConfirmAndContinueIfDirtyAsync())
            return false;

        _projectModel.Reset();
        _settingsService.Settings.LastOpenedFile = "";
        return true;
    }

    /// <summary>
    /// Unsaved changes: saves them (Save As for a new project) or discards them, as the user chooses. False if the
    /// user canceled, also when the Save As dialog was canceled.
    /// </summary>
    public async Task<bool> ConfirmAndContinueIfDirtyAsync()
    {
        if (_projectModel.IsDirty)
        {
            var choice = await _messageBoxService.ShowSaveChangesAsync(
                LocalizationService.Instance["Title_UnsavedChanges"],
                LocalizationService.Instance["Text_UnsavedChanges"]);

            switch (choice)
            {
                case SaveChangesChoice.Save:
                    if (!await SaveAsync(_projectModel.ToDto()))
                        return false;
                    break;
                case SaveChangesChoice.Cancel:
                    return false;
            }
        }
        _projectModel.MarkSaved();
        return true;
    }
}