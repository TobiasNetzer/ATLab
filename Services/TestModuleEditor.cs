using System;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class TestModuleEditor : ITestModuleEditor
{
    private readonly ProjectModel _projectModel;
    private readonly IHardwareInfo _hardwareInfo;

    public TestModuleEditor(ProjectModel projectModel, IHardwareAccessor hardwareAccessor)
    {
        _projectModel = projectModel;
        _hardwareInfo = hardwareAccessor.Hardware.HardwareInfo;
    }

    public TestModule AddModule(int insertIndex)
    {
        var module = new TestModule
        {
            Name = TestModule.CreateDefaultName(_projectModel.Modules, LocalizationService.Instance["Text_DefaultModuleName"])
        };

        var step = new TestStep();
        step.InitializeRuntimeState(_hardwareInfo);
        module.TestSteps.Add(step);

        _projectModel.Modules.Insert(insertIndex, module);

        return module;
    }

    public TestModule DuplicateModule(TestModule module)
    {
        foreach (var step in module.TestSteps)
            step.UpdateDtos();

        var clone = module.Clone();
        clone.Name = $"{module.Name} ({LocalizationService.Instance["Text_CopySuffix"]})";

        foreach (var step in clone.TestSteps)
            step.InitializeRuntimeState(_hardwareInfo);

        var index = _projectModel.Modules.IndexOf(module);
        _projectModel.Modules.Insert(index + 1, clone);

        return clone;
    }

    public int RemoveModule(TestModule module)
    {
        var index = _projectModel.Modules.IndexOf(module);

        if (index < 0)
            return -1;

        _projectModel.Modules.RemoveAt(index);

        if (_projectModel.Modules.Count == 0)
            return -1;

        return Math.Min(index, _projectModel.Modules.Count - 1);
    }

    public bool MoveModuleUp(TestModule module)
    {
        var index = _projectModel.Modules.IndexOf(module);

        if (index <= 0)
            return false;

        _projectModel.Modules.Move(index, index - 1);

        return true;
    }

    public bool MoveModuleDown(TestModule module)
    {
        var index = _projectModel.Modules.IndexOf(module);

        if (index < 0 || index >= _projectModel.Modules.Count - 1)
            return false;

        _projectModel.Modules.Move(index, index + 1);

        return true;
    }
}