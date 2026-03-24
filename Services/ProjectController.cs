using System;
using System.Linq;
using System.Threading.Tasks;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class ProjectController
{
    private readonly IProjectService _projectService;
    private readonly IErrorService _errorService;
    private readonly ProjectSettings _projectSettings;
    private readonly ProjectDocumentation _projectDocumentation;
    private readonly DeviceManagerViewModel _deviceManager;

    public ProjectController(
        IProjectService projectService,
        IErrorService errorService,
        ProjectSettings projectSettings,
        ProjectDocumentation projectDocumentation,
        DeviceManagerViewModel deviceManager)
    {
        _projectService = projectService;
        _errorService = errorService;
        _projectSettings = projectSettings;
        _projectDocumentation = projectDocumentation;
        _deviceManager = deviceManager;
    }

    public async Task NewProjectAsync(TestingTabViewModel vm)
    {
        if (!await _projectService.NewProjectAsync())
            return;

        using (vm.SuppressDirtyTracking())
        {
            vm.TestSteps.Clear();
            vm.TestHardwareRelayChannels.ResetToDefault();
            _projectSettings.ResetToDefault();
            _projectDocumentation.ResetToDefault();
            _deviceManager.Devices.Clear();

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
                ApplyDto(vm, dto);
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
                ApplyDto(vm, dto);
        }
        catch (Exception ex)
        {
            _errorService.AddError("Failed to load file: " + ex.Message);
        }

        vm.ResetTestCounters();
    }

    private void ApplyDto(TestingTabViewModel vm, AtlabFileDto dto)
    {
        using (vm.SuppressDirtyTracking())
        {
            vm.TestSteps.Clear();

            foreach (var step in dto.TestSteps)
            {
                var vmStep = new TestStepViewModel(step, vm.TestHardwareRelayChannels.HardwareInfo);
                vmStep.PropertyChanged += vm.OnStepPropertyChanged;
                vm.TestSteps.Add(vmStep);
            }

            _projectSettings.CopyFrom(dto.ProjectSettings);
            _projectDocumentation.CopyFrom(dto.ProjectDocumentation);

            vm.TestHardwareRelayChannels.ApplyChannelNames(
                dto.StimChannelNames,
                dto.ExtStimChannelNames,
                dto.MeasChannelNames);

            _deviceManager.Devices.Clear();
            foreach (var device in dto.Devices)
                _deviceManager.Devices.Add(device);

            vm.SelectedStepIndex = 0;
        }
    }

    private AtlabFileDto CaptureCurrentState(TestingTabViewModel vm)
    {
        foreach (var stepVm in vm.TestSteps)
            stepVm.TestStep.UpdateDtos();

        return new AtlabFileDto
        {
            TestSteps = vm.TestSteps.Select(s => s.TestStep).ToList(),
            StimChannelNames = vm.TestHardwareRelayChannels.GetStimNames(),
            ExtStimChannelNames = vm.TestHardwareRelayChannels.GetExtStimNames(),
            MeasChannelNames = vm.TestHardwareRelayChannels.GetMeasNames(),
            Devices = _deviceManager.Devices.ToList(),
            ProjectSettings = _projectSettings,
            ProjectDocumentation = _projectDocumentation
        };
    }
    
    public void MarkDirty()
    {
        _projectService.IsDirty = true;
    }
}
