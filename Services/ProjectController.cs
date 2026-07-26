using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class ProjectController : IProjectController
{
    private readonly IProjectDocumentService _projectDocumentService;
    private readonly IErrorService _errorService;
    private readonly IHardwareInfo _hardwareInfo;
    private readonly ProjectModel _projectModel;
    private readonly IMessageBoxService _messageBoxService;

    public ProjectController(
        IProjectDocumentService projectDocumentService,
        IErrorService errorService,
        IHardwareInfo hardwareInfo,
        ProjectModel projectModel,
        IMessageBoxService messageBoxService)
    {
        _projectDocumentService = projectDocumentService;
        _errorService = errorService;
        _hardwareInfo = hardwareInfo;
        _projectModel = projectModel;
        _messageBoxService = messageBoxService;
    }

    public async Task NewProjectAsync(TestingTabViewModel vm)
    {
        if (!await _projectDocumentService.NewProjectAsync())
            return;

        using (_projectModel.SuppressDirtyTracking())
        {
            vm.TestSteps.Clear();
            _projectModel.Reset();
            
            vm.SelectedStepIndex = -1;
            vm.AddInitialStep();
            vm.ResetTestCounters();
        }
    }

    public async Task SaveFileAsync()
    {
        var dto = CaptureCurrentState();
        await _projectDocumentService.SaveAsync(dto);
    }

    public async Task SaveFileAsAsync()
    {
        var dto = CaptureCurrentState();
        await _projectDocumentService.SaveAsAsync(dto);
    }

    public async Task LoadFileWithDialogAsync(TestingTabViewModel vm)
    {
        try
        {
            var dto = await _projectDocumentService.OpenFileAsync();
            if (dto != null)
            {
                await CheckForHardwareCompatibility(_hardwareInfo, dto);
                ApplyDto(dto);
                vm.SelectedStepIndex = 0;
            }
        }
        catch (Exception ex)
        {
            _errorService.AddError("Failed to load file: " + ex.Message);
        }

        vm.ResetTestCounters();
    }

    public async Task LoadFileAsync(TestingTabViewModel vm, string path)
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
                vm.SelectedStepIndex = 0;
            }
                
        }
        catch (Exception ex)
        {
            _errorService.AddError($"Failed to load file {path}: " + ex.Message);
        }

        vm.ResetTestCounters();
    }

    private void ApplyDto(AtlabFileDto dto)
    {
        using (_projectModel.SuppressDirtyTracking())
        {
            _projectModel.Load(dto);
        }
    }

    private AtlabFileDto CaptureCurrentState()
    {
        foreach (var step in _projectModel.TestSteps)
            step.UpdateDtos();

        return new AtlabFileDto
        {
            TestSteps = _projectModel.TestSteps.ToList(),
            StimChannelNames = _projectModel.StimChannelNames.ToList(),
            ExtStimChannelNames = _projectModel.ExtStimChannelNames.ToList(),
            MeasChannelNames = _projectModel.MeasChannelNames.ToList(),
            RuntimeVariables = _projectModel.RuntimeVariables.ToList(),
            Devices = _projectModel.Devices.ToList(),
            ProjectSettings = _projectModel.Settings,
            ProjectDocumentation = _projectModel.Documentation,
            DeviceUnderTestInfo = _projectModel.DeviceUnderTestInfo
        };
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