using System.Collections.Generic;
using System.Linq;
using ATLab.Interfaces;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Services;

public class TestStepEditor : ITestStepEditor
{
    private readonly TestHardwareRelayChannelsViewModel _hardware;

    private List<TestStep>? _clipboard;
    private bool _clipboardIsCut;

    public TestStepEditor(TestHardwareRelayChannelsViewModel hardware)
    {
        _hardware = hardware;
    }

    public bool HasClipboard => _clipboard != null && _clipboard.Count > 0;
    
    public void AddStep(TestingTabViewModel vm)
    {
        var index = ComputeInsertIndex(vm);

        var newStep = new TestStepViewModel(new TestStep(), _hardware.HardwareInfo);
        newStep.PropertyChanged += vm.OnStepPropertyChanged;

        vm.TestSteps.Insert(index, newStep);
        Renumber(vm);

        vm.SelectedStep = newStep;
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

        var duplicates = new List<TestStepViewModel>();

        foreach (var step in ordered)
        {
            step.TestStep.UpdateDtos();
            var clone = step.TestStep.Clone();

            var vmStep = new TestStepViewModel(clone, _hardware.HardwareInfo);
            vmStep.PropertyChanged += vm.OnStepPropertyChanged;

            vm.TestSteps.Insert(insertIndex++, vmStep);
            duplicates.Add(vmStep);
        }

        Renumber(vm);

        vm.SelectedSteps.Clear();
        foreach (var d in duplicates)
            vm.SelectedSteps.Add(d);

        vm.SelectedStep = duplicates.Last();
    }

    public void CopySteps(TestingTabViewModel vm)
    {
        if (vm.SelectedSteps.Count == 0)
            return;

        foreach (var step in vm.SelectedSteps)
            step.TestStep.UpdateDtos();

        _clipboard = vm.SelectedSteps
            .OrderBy(s => vm.TestSteps.IndexOf(s))
            .Select(s => s.TestStep.Clone(preserveId: false))
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
            : vm.SelectedSteps.Select(s => vm.TestSteps.IndexOf(s)).Max() + 1;

        var pasted = new List<TestStepViewModel>();

        foreach (var model in _clipboard)
        {
            var clone = model.Clone(preserveId: _clipboardIsCut);
            
            var vmStep = new TestStepViewModel(clone, _hardware.HardwareInfo);
            vmStep.PropertyChanged += vm.OnStepPropertyChanged;

            vm.TestSteps.Insert(insertIndex++, vmStep);
            pasted.Add(vmStep);
        }

        Renumber(vm);

        vm.SelectedSteps.Clear();
        foreach (var p in pasted)
            vm.SelectedSteps.Add(p);

        vm.SelectedStep = pasted.Last();

        if (!_clipboardIsCut)
            return;
        
        _clipboard = null;
        _clipboardIsCut = false;
        vm.NotifyPasteChanged();
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
            .ToList();

        foreach (var step in toRemove)
        {
            step.PropertyChanged -= vm.OnStepPropertyChanged;
            vm.TestSteps.Remove(step);
        }

        Renumber(vm);

        vm.SelectedSteps.Clear();
        vm.SelectedStep = null;

        vm.NotifyPasteChanged();
    }

    public void RemoveSteps(TestingTabViewModel vm)
    {
        var toRemove = vm.SelectedSteps
            .OrderByDescending(s => vm.TestSteps.IndexOf(s))
            .ToList();

        foreach (var step in toRemove)
        {
            step.PropertyChanged -= vm.OnStepPropertyChanged;
            vm.TestSteps.Remove(step);
        }

        Renumber(vm);
    }

    public void MoveStepUp(TestingTabViewModel vm)
    {
        if (vm.SelectedStep == null || vm.SelectedStepIndex <= 0)
            return;

        var step = vm.SelectedStep;
        var oldIndex = vm.SelectedStepIndex;
        var newIndex = oldIndex - 1;

        vm.TestSteps.RemoveAt(oldIndex);
        vm.TestSteps.Insert(newIndex, step);

        Renumber(vm);
        vm.SelectedStepIndex = newIndex;
    }

    public void MoveStepDown(TestingTabViewModel vm)
    {
        if (vm.SelectedStep == null || vm.SelectedStepIndex < 0 || vm.SelectedStepIndex >= vm.TestSteps.Count - 1)
            return;

        var step = vm.SelectedStep;
        var oldIndex = vm.SelectedStepIndex;
        var newIndex = oldIndex + 1;

        vm.TestSteps.RemoveAt(oldIndex);
        vm.TestSteps.Insert(newIndex, step);

        Renumber(vm);
        vm.SelectedStepIndex = newIndex;
    }

    public int ComputeInsertIndex(TestingTabViewModel vm)
    {
        if (vm.SelectedStep == null)
            return 0;

        if (vm.SelectedSteps.Count == 0)
            return 0;

        return vm.SelectedSteps
            .Select(s => vm.TestSteps.IndexOf(s))
            .Max() + 1;
    }

    public void Renumber(TestingTabViewModel vm)
    {
        for (var i = 0; i < vm.TestSteps.Count; i++)
            vm.TestSteps[i].TestStep.Number = i + 1;
    }
}