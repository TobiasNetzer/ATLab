using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

/// <summary>
/// Project commands of the toolbar: creates, opens and saves projects and checks a loaded project against the
/// connected hardware.
/// </summary>
public class ProjectController : IProjectController
{
    private readonly IProjectDocumentService _projectDocumentService;
    private readonly ILoggingService _loggingService;
    private readonly IHardwareInfo _hardwareInfo;
    private readonly ProjectModel _projectModel;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IScriptRepository _scriptRepository;

    public ProjectController(
        IProjectDocumentService projectDocumentService,
        ILoggingService loggingService,
        IHardwareAccessor hardwareAccessor,
        ProjectModel projectModel,
        IMessageBoxService messageBoxService,
        IScriptRepository scriptRepository)
    {
        _projectDocumentService = projectDocumentService;
        _loggingService = loggingService;
        _hardwareInfo = hardwareAccessor.Hardware.HardwareInfo;
        _projectModel = projectModel;
        _messageBoxService = messageBoxService;
        _scriptRepository = scriptRepository;
        
        _scriptRepository.RepositoryFolderChanged += (_, folderPath) =>
        {
            _projectModel.ScriptFolderPath = folderPath;
        };
    }

    public async Task<bool> NewProjectAsync()
    {
        if (!await ResetProjectAsync())
            return false;

        _projectModel.IsProjectOpen = true;
        return true;
    }

    /// <summary>Closes the project after asking about unsaved changes; the main window returns to the welcome screen.</summary>
    public async Task<bool> CloseProjectAsync()
    {
        if (!await _projectDocumentService.CloseProjectAsync())
            return false;

        _scriptRepository.SetRepositoryFolder(null);
        _projectModel.IsProjectOpen = false;
        return true;
    }

    /// <summary>
    /// Replaces the project with an empty one after asking about unsaved changes. Leaves the open state alone, so a
    /// canceled or failed open from the welcome screen stays there.
    /// </summary>
    private async Task<bool> ResetProjectAsync()
    {
        if (!await _projectDocumentService.NewProjectAsync())
            return false;
        
        _projectModel.CreateEmptyProject();
        _scriptRepository.SetRepositoryFolder(null);
        return true;
    }

    public async Task SaveFileAsync()
    {
        await _projectDocumentService.SaveAsync(_projectModel.ToDto());
    }

    public async Task SaveFileAsAsync()
    {
        await _projectDocumentService.SaveAsAsync(_projectModel.ToDto());
    }

    public async Task LoadFileWithDialogAsync()
    {
        if (!await ResetProjectAsync())
            return;
        
        try
        {
            var dto = await _projectDocumentService.OpenFileAsync();
            if (dto != null)
            {
                await CheckForHardwareCompatibility(_hardwareInfo, dto);
                ApplyDto(dto);
                _projectModel.IsProjectOpen = true;
            }
            
        }
        catch (Exception ex)
        {
            _loggingService.Error("Failed to load file: " + ex.Message);
        }
    }

    public async Task LoadFileAsync(string path)
    {
        try
        {
            if (!await ResetProjectAsync())
                return;

            var dto = await _projectDocumentService.OpenAsync(path);
            if (dto != null)
            {
                await CheckForHardwareCompatibility(_hardwareInfo, dto);
                ApplyDto(dto);
                _projectModel.IsProjectOpen = true;
            }
                
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Failed to load file {path}: " + ex.Message);
        }
    }

    private void ApplyDto(AtlabFileDto dto)
    {
        using (_projectModel.SuppressDirtyTracking())
        {
            _projectModel.Load(dto);
            _scriptRepository.SetRepositoryFolder(_projectModel.ScriptFolderPath);
        }
    }

    private async Task CheckForHardwareCompatibility(IHardwareInfo hardwareInfo, AtlabFileDto dto)
    {
        List<string> warnings = [];

        var allSteps = dto.Modules.SelectMany(m => m.TestSteps).ToList();

        var highestUsedMatrixChannel =
            allSteps
                .Select(s => Math.Max(s.MatrixState.ActiveChannelHigh, s.MatrixState.ActiveChannelLow))
                .DefaultIfEmpty(-1)
                .Max();

        var highestUsedStimChannel =
            allSteps
                .Where(s => s.StimState?.EnabledChannels != null)
                .SelectMany(s => s.StimState!.EnabledChannels)
                .DefaultIfEmpty(-1)
                .Max();

        var highestUsedExtStimChannel =
            allSteps
                .Where(s => s.ExtStimState?.EnabledChannels != null)
                .SelectMany(s => s.ExtStimState!.EnabledChannels)
                .DefaultIfEmpty(-1)
                .Max();
        
        var localization = LocalizationService.Instance;

        if (hardwareInfo.MeasChannelCount < highestUsedMatrixChannel)
            warnings.Add(string.Format(localization["Text_MissingMatrixChannels"], hardwareInfo.MeasChannelCount, highestUsedMatrixChannel));

        if (hardwareInfo.StimChannelCount < highestUsedStimChannel)
            warnings.Add(string.Format(localization["Text_MissingStimChannels"], hardwareInfo.StimChannelCount, highestUsedStimChannel));

        if (hardwareInfo.ExtStimChannelCount < highestUsedExtStimChannel)
            warnings.Add(string.Format(localization["Text_MissingExtStimChannels"], hardwareInfo.ExtStimChannelCount, highestUsedExtStimChannel));

        if (warnings.Count > 0)
        {
            warnings.Insert(0, localization["Text_MissingHardwareChannels"]);
            warnings.Insert(1,"");
            warnings.Add("");
            warnings.Add(localization["Text_UnavailableChannelsLostOnSave"]);

            var message = string.Join(Environment.NewLine, warnings);
            await _messageBoxService.ShowMessageAsync(localization["Title_Warning"], message);
        }
    }
}