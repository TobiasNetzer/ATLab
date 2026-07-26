using System.Collections.Generic;
using System.Linq;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class TestStepEditor : ITestStepEditor
{
    private readonly IHardwareInfo _hardwareInfo;
    private readonly ProjectModel _projectModel;

    private List<TestStep>? _clipboard;
    private bool _clipboardIsCut;

    public TestStepEditor(
        IHardwareInfo hardwareInfo,
        ProjectModel projectModel)
    {
        _hardwareInfo = hardwareInfo;
        _projectModel = projectModel;
    }

    public bool HasClipboard => _clipboard != null && _clipboard.Count > 0;

    public void AddStep(TestingTabViewModel vm)
    {
        var index = ComputeInsertIndex(vm);

        var step = new TestStep();

        _projectModel.TestSteps.Insert(index, step);

        Renumber();

        vm.SelectedStep = vm.TestSteps[index];
    }

    public TestStepViewModel CreateViewModel(TestStep step)
    {
        var vm = new TestStepViewModel(step, _hardwareInfo);
        return vm;
    }

    public void DuplicateSteps(TestingTabViewModel vm)
    {
        if (vm.SelectedSteps.Count == 0)
            return;

        var insertIndex = vm.SelectedSteps
            .Select(s => vm.TestSteps.IndexOf(s))
            .Max() + 1;

        var ordered = vm.SelectedSteps
            .OrderBy(s => vm.TestSteps.IndexOf(s))
            .ToList();

        foreach (var vmStep in ordered)
        {
            vmStep.TestStep.UpdateDtos();

            var clone = vmStep.TestStep.Clone();

            _projectModel.TestSteps.Insert(insertIndex++, clone);
        }

        Renumber();

        vm.SelectedSteps.Clear();

        for (int i = insertIndex - ordered.Count; i < insertIndex; i++)
            vm.SelectedSteps.Add(vm.TestSteps[i]);

        vm.SelectedStep = vm.SelectedSteps.Last();
    }

    public void CopySteps(TestingTabViewModel vm)
    {
        if (vm.SelectedSteps.Count == 0)
            return;

        foreach (var step in vm.SelectedSteps)
            step.TestStep.UpdateDtos();

        _clipboard = vm.SelectedSteps
            .OrderBy(s => vm.TestSteps.IndexOf(s))
            .Select(s => s.TestStep.Clone(false))
            .ToList();

        _clipboardIsCut = false;

        vm.NotifyPasteChanged();
    }

    public void PasteSteps(TestingTabViewModel vm)
    {
        if (_clipboard == null || _clipboard.Count == 0)
            return;

        var insertIndex = vm.SelectedSteps.Count == 0
            ? 0
            : vm.SelectedSteps
                .Select(s => vm.TestSteps.IndexOf(s))
                .Max() + 1;

        var startIndex = insertIndex;

        foreach (var model in _clipboard)
        {
            var clone = model.Clone(_clipboardIsCut);

            _projectModel.TestSteps.Insert(insertIndex++, clone);
        }

        Renumber();

        vm.SelectedSteps.Clear();

        for (int i = startIndex; i < insertIndex; i++)
            vm.SelectedSteps.Add(vm.TestSteps[i]);

        vm.SelectedStep = vm.SelectedSteps.Last();

        if (_clipboardIsCut)
        {
            _clipboard = null;
            _clipboardIsCut = false;
            vm.NotifyPasteChanged();
        }
    }
    
        public void CutSteps(TestingTabViewModel vm)
    {
        if (vm.SelectedSteps.Count == 0)
            return;

        foreach (var step in vm.SelectedSteps)
            step.TestStep.UpdateDtos();

        _clipboard = vm.SelectedSteps
            .OrderBy(s => vm.TestSteps.IndexOf(s))
            .Select(s => s.TestStep.Clone(preserveId: true))
            .ToList();

        _clipboardIsCut = true;

        var toRemove = vm.SelectedSteps
            .OrderByDescending(s => vm.TestSteps.IndexOf(s))
            .Select(s => s.TestStep)
            .ToList();

        foreach (var step in toRemove)
            _projectModel.TestSteps.Remove(step);

        Renumber();

        vm.SelectedSteps.Clear();
        vm.SelectedStep = null;

        vm.NotifyPasteChanged();
    }

    public void RemoveSteps(TestingTabViewModel vm)
    {
        if (vm.SelectedSteps.Count == 0)
            return;

        var toRemove = vm.SelectedSteps
            .OrderByDescending(s => vm.TestSteps.IndexOf(s))
            .Select(s => s.TestStep)
            .ToList();

        foreach (var step in toRemove)
            _projectModel.TestSteps.Remove(step);

        Renumber();

        vm.SelectedSteps.Clear();

        if (vm.TestSteps.Count > 0)
        {
            var index = System.Math.Min(vm.SelectedStepIndex, vm.TestSteps.Count - 1);
            vm.SelectedStep = vm.TestSteps[index];
        }
        else
        {
            vm.SelectedStep = null;
        }
    }

    public void MoveStepUp(TestingTabViewModel vm)
    {
        if (vm.SelectedStep == null)
            return;

        var oldIndex = vm.SelectedStepIndex;

        if (oldIndex <= 0)
            return;

        _projectModel.TestSteps.Move(oldIndex, oldIndex - 1);

        Renumber();

        vm.SelectedStepIndex = oldIndex - 1;
        vm.SelectedStep = vm.TestSteps[oldIndex - 1];
    }

    public void MoveStepDown(TestingTabViewModel vm)
    {
        if (vm.SelectedStep == null)
            return;

        var oldIndex = vm.SelectedStepIndex;

        if (oldIndex >= _projectModel.TestSteps.Count - 1)
            return;

        _projectModel.TestSteps.Move(oldIndex, oldIndex + 1);

        Renumber();

        vm.SelectedStepIndex = oldIndex + 1;
        vm.SelectedStep = vm.TestSteps[oldIndex + 1];
    }

    private int ComputeInsertIndex(TestingTabViewModel vm)
    {
        if (vm.SelectedSteps.Count == 0)
            return _projectModel.TestSteps.Count;

        return vm.SelectedSteps
            .Select(s => vm.TestSteps.IndexOf(s))
            .Max() + 1;
    }

    private void Renumber()
    {
        for (int i = 0; i < _projectModel.TestSteps.Count; i++)
            _projectModel.TestSteps[i].Number = i + 1;
    }
}