using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class ProjectController : IProjectController
{
    private readonly IProjectDocumentService _projectDocumentService;
    private readonly IErrorService _errorService;
    private readonly IHardwareInfo _hardwareInfo;
    private readonly ProjectModel _projectModel;
    private readonly IMessageBoxService _messageBoxService;
    private readonly IScriptRepository _scriptRepository;

    public ProjectController(
        IProjectDocumentService projectDocumentService,
        IErrorService errorService,
        IHardwareInfo hardwareInfo,
        ProjectModel projectModel,
        IMessageBoxService messageBoxService,
        IScriptRepository scriptRepository)
    {
        _projectDocumentService = projectDocumentService;
        _errorService = errorService;
        _hardwareInfo = hardwareInfo;
        _projectModel = projectModel;
        _messageBoxService = messageBoxService;
        _scriptRepository = scriptRepository;
        
        _scriptRepository.RepositoryFolderChanged += (_, folderPath) =>
        {
            _projectModel.ScriptRepositoryPath = folderPath;
        };
    }

    public async Task NewProjectAsync()
    {
        if (!await _projectDocumentService.NewProjectAsync())
            return;
        
        _projectModel.CreateEmptyProject();
        _scriptRepository.SetRepositoryFolder(null);
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
        try
        {
            var dto = await _projectDocumentService.OpenFileAsync();
            if (dto != null)
            {
                await CheckForHardwareCompatibility(_hardwareInfo, dto);
                ApplyDto(dto);
            }
        }
        catch (Exception ex)
        {
            _errorService.AddError("Failed to load file: " + ex.Message);
        }
    }

    public async Task LoadFileAsync(string path)
    {
        try
        {
            if (!await _projectDocumentService.ConfirmAndContinueIfDirtyAsync())
                return;

            var dto = await _projectDocumentService.OpenAsync(path);
            if (dto != null)
            {
                await CheckForHardwareCompatibility(_hardwareInfo, dto);
                ApplyDto(dto);
            }
                
        }
        catch (Exception ex)
        {
            _errorService.AddError($"Failed to load file {path}: " + ex.Message);
        }
    }

    private void ApplyDto(AtlabFileDto dto)
    {
        using (_projectModel.SuppressDirtyTracking())
        {
            _projectModel.Load(dto);
            _scriptRepository.SetRepositoryFolder(_projectModel.ScriptRepositoryPath);
        }
    }

    private async Task CheckForHardwareCompatibility(IHardwareInfo hardwareInfo, AtlabFileDto dto)
    {
        List<string> warnings = [];

        var highestUsedMatrixChannel =
            dto.TestSteps
                .Select(s => Math.Max(s.MatrixState.ActiveChannelHigh, s.MatrixState.ActiveChannelLow))
                .DefaultIfEmpty(-1)
                .Max();

        var highestUsedStimChannel =
            dto.TestSteps
                .Where(s => s.StimState?.EnabledChannels != null)
                .SelectMany(s => s.StimState!.EnabledChannels)
                .DefaultIfEmpty(-1)
                .Max();

        var highestUsedExtStimChannel =
            dto.TestSteps
                .Where(s => s.ExtStimState?.EnabledChannels != null)
                .SelectMany(s => s.ExtStimState!.EnabledChannels)
                .DefaultIfEmpty(-1)
                .Max();
        
        if (hardwareInfo.MeasChannelCount < highestUsedMatrixChannel)
            warnings.Add($"- Matrix channels (available: {hardwareInfo.MeasChannelCount}/{highestUsedMatrixChannel})");

        if (hardwareInfo.StimChannelCount < highestUsedStimChannel)
            warnings.Add($"- Stimulation channels (available: {hardwareInfo.StimChannelCount}/{highestUsedStimChannel})");

        if (hardwareInfo.ExtStimChannelCount < highestUsedExtStimChannel)
            warnings.Add($"- External Stimulation channels (available: {hardwareInfo.ExtStimChannelCount}/{highestUsedExtStimChannel})");
        
        if (warnings.Count > 0)
        {
            warnings.Insert(0, "Connected test hardware is missing required channels:");
            warnings.Insert(1,"");
            warnings.Add("");
            warnings.Add("All unavailable channels will be permanently lost upon saving!");

            var message = string.Join(Environment.NewLine, warnings);
            await _messageBoxService.ShowMessageAsync("Warning", message);
        }
    }
}