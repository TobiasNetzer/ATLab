using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Helpers;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class ProjectController : IProjectController
{
    private readonly IProjectService _projectService;
    private readonly IErrorService _errorService;
    private readonly ProjectSettings _projectSettings;
    private readonly ProjectDocumentation _projectDocumentation;
    private readonly DeviceUnderTestInfo _deviceUnderTestInfo;
    private readonly DeviceManagerViewModel _deviceManager;
    private readonly IMessageBoxService _messageBoxService;
    private readonly RuntimeVariableEditorViewModel _runtimeVariableEditorViewModel;

    public ProjectController(
        IProjectService projectService,
        IErrorService errorService,
        ProjectSettings projectSettings,
        ProjectDocumentation projectDocumentation,
        DeviceUnderTestInfo deviceUnderTestInfo,
        DeviceManagerViewModel deviceManager,
        IMessageBoxService messageBoxService,
        RuntimeVariableEditorViewModel runtimeVariableEditorViewModel)
    {
        _projectService = projectService;
        _errorService = errorService;
        _projectSettings = projectSettings;
        _projectDocumentation = projectDocumentation;
        _deviceUnderTestInfo = deviceUnderTestInfo;
        _deviceManager = deviceManager;
        _messageBoxService = messageBoxService;
        _runtimeVariableEditorViewModel = runtimeVariableEditorViewModel;
    }

    public async Task NewProjectAsync(TestingTabViewModel vm)
    {
        if (!await _projectService.NewProjectAsync())
            return;

        using (vm.SuppressDirtyTracking())
        {
            vm.TestSteps.Clear();
            vm.EditorWorkspace.TestHardwareRelayChannels.ResetToDefault();
            _projectSettings.ResetToDefault();
            _projectDocumentation.ResetToDefault();
            _deviceUnderTestInfo.ResetToDefault();
            _deviceManager.Devices.Clear();
            _runtimeVariableEditorViewModel.RuntimeVariables.Clear();

            _projectService.UpdateLastSavedState(CaptureCurrentState(vm));

            vm.SelectedStepIndex = -1;
            vm.AddInitialStep();
            vm.ResetTestCounters();
        }
    }

    public async Task SaveFileAsync(TestingTabViewModel vm)
    {
        var dto = CaptureCurrentState(vm);
        await _projectService.SaveAsync(dto);
    }

    public async Task SaveFileAsAsync(TestingTabViewModel vm)
    {
        var dto = CaptureCurrentState(vm);
        await _projectService.SaveAsAsync(dto);
    }

    public async Task LoadFileWithDialogAsync(TestingTabViewModel vm)
    {
        try
        {
            var dto = await _projectService.OpenFileAsync();
            if (dto != null)
            {
                await CheckForHardwareCompatibility(vm.EditorWorkspace.TestHardwareRelayChannels.HardwareInfo, dto);
                ApplyDto(vm, dto);
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
            if (!await _projectService.ConfirmAndContinueIfDirtyAsync())
                return;

            var dto = await _projectService.LoadAsync(path);
            if (dto != null)
            {
                await CheckForHardwareCompatibility(vm.EditorWorkspace.TestHardwareRelayChannels.HardwareInfo, dto);
                ApplyDto(vm, dto);
            }
                
        }
        catch (Exception ex)
        {
            _errorService.AddError($"Failed to load file {path}: " + ex.Message);
        }

        vm.ResetTestCounters();
    }

    public void ApplyDto(TestingTabViewModel vm, AtlabFileDto dto)
    {
        using (vm.SuppressDirtyTracking())
        {
            vm.TestSteps.Clear();

            foreach (var step in dto.TestSteps)
            {
                var vmStep = new TestStepViewModel(step, vm.EditorWorkspace.TestHardwareRelayChannels.HardwareInfo);
                vmStep.PropertyChanged += vm.OnStepPropertyChanged;
                vm.TestSteps.Add(vmStep);
            }

            _projectSettings.CopyFrom(dto.ProjectSettings);
            _projectDocumentation.CopyFrom(dto.ProjectDocumentation);
            _deviceUnderTestInfo.CopyFrom(dto.DeviceUnderTestInfo);

            vm.EditorWorkspace.TestHardwareRelayChannels.ApplyChannelNames(
                dto.StimChannelNames,
                dto.ExtStimChannelNames,
                dto.MeasChannelNames);

            _deviceManager.Devices.Clear();
            foreach (var device in dto.Devices)
                _deviceManager.Devices.Add(device);
            
            _runtimeVariableEditorViewModel.RuntimeVariables.Clear();
            foreach (var variable in dto.RuntimeVariables)
                _runtimeVariableEditorViewModel.RuntimeVariables.Add(variable);
            
            foreach (var stepVm in vm.TestSteps)
            {
                TestStepRuntimeInitializer.InitializeRuntimeValues(stepVm.TestStep);
            }
            
            vm.SelectedStepIndex = 0;
        }
    }

    public AtlabFileDto CaptureCurrentState(TestingTabViewModel vm)
    {
        foreach (var stepVm in vm.TestSteps)
            stepVm.TestStep.UpdateDtos();

        return new AtlabFileDto
        {
            TestSteps = vm.TestSteps.Select(s => s.TestStep).ToList(),
            StimChannelNames = vm.EditorWorkspace.TestHardwareRelayChannels.GetStimNames(),
            ExtStimChannelNames = vm.EditorWorkspace.TestHardwareRelayChannels.GetExtStimNames(),
            MeasChannelNames = vm.EditorWorkspace.TestHardwareRelayChannels.GetMeasNames(),
            RuntimeVariables = _runtimeVariableEditorViewModel.RuntimeVariables.ToList(),
            Devices = _deviceManager.Devices.ToList(),
            ProjectSettings = _projectSettings,
            ProjectDocumentation = _projectDocumentation,
            DeviceUnderTestInfo = _deviceUnderTestInfo
        };
    }

    public async Task CheckForHardwareCompatibility(IHardwareInfo hardwareInfo, AtlabFileDto dto)
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
    
    public void MarkDirty()
    {
        _projectService.IsDirty = true;
    }
}