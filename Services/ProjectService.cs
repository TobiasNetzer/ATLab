using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class ProjectService : IProjectService
{
    private readonly IFileDialogService _fileDialogService;
    private readonly IFileService _fileService;
    private readonly ISettingsService _settingsService;
    private readonly IMessageBoxService _messageBoxService;

    private string? _currentFilePath;
    public string? CurrentFilePath
    {
        get => _currentFilePath;
        set => SetProperty(ref _currentFilePath, value);
    }

    private bool _isDirty;
    public bool IsDirty
    {
        get => _isDirty;
        set => SetProperty(ref _isDirty, value);
    }

    private string? _lastSavedJson;

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
        if (!await ConfirmAndContinueIfDirtyAsync()) return null;

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
        if (dto != null)
        {
            CurrentFilePath = path;
            _lastSavedJson = _fileService.Serialize(dto);
            _settingsService.Settings.LastOpenedFile = path;
            IsDirty = false;
        }
        return dto;
    }

    public async Task<bool> SaveAsync(AtlabFileDto dto)
    {
        if (!string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            await _fileService.SaveAsync(CurrentFilePath, dto);
            _lastSavedJson = _fileService.Serialize(dto);
            IsDirty = false;
            return true;
        }

        return await SaveAsAsync(dto);
    }

    public async Task<bool> SaveAsAsync(AtlabFileDto dto)
    {
        var file = await _fileDialogService.SaveFileAsync("ATLab files", "Test.atlab", "atlab", new[] { "atlab" });

        if (file is not null)
        {
            await _fileService.SaveAsync(file.Path.LocalPath, dto);
            _lastSavedJson = _fileService.Serialize(dto);
            CurrentFilePath = file.Path.LocalPath;
            _settingsService.Settings.LastOpenedFile = file.Path.LocalPath;
            IsDirty = false;
            return true;
        }

        return false;
    }

    public async Task<bool> NewProjectAsync()
    {
        if (!await ConfirmAndContinueIfDirtyAsync()) return false;

        CurrentFilePath = null;
        _lastSavedJson = null; // Will be set by UpdateLastSavedState when clearing viewmodel
        IsDirty = false;
        return true;
    }

    public async Task<bool> ConfirmAndContinueIfDirtyAsync()
    {
        if (IsDirty)
        {
            var result = await _messageBoxService.ShowConfirmationAsync("Unsaved Changes", "You have unsaved changes. Continue without saving?");
            if (!result) return false;
        }

        IsDirty = false;
        return true;
    }

    public void UpdateLastSavedState(AtlabFileDto dto)
    {
        _lastSavedJson = _fileService.Serialize(dto);
        IsDirty = false;
    }

    public bool IsStateChanged(AtlabFileDto currentDto)
    {
        if (_lastSavedJson == null) return false;
        var currentJson = _fileService.Serialize(currentDto);
        IsDirty = currentJson != _lastSavedJson;
        return IsDirty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
