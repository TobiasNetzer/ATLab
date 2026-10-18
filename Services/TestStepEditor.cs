using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class TestStepEditor : ITestStepEditor
{
    private readonly IHardwareInfo _hardwareInfo;

    private List<TestStep>? _clipboard;
    private bool _clipboardIsCut;

    public bool CanPaste => _clipboard != null && _clipboard.Count > 0;

    public TestStepEditor(IHardwareAccessor hardwareAccessor)
    {
        _hardwareInfo = hardwareAccessor.Hardware.HardwareInfo;
    }

    public TestStep AddStep(TestModule module, int insertIndex)
    {
        var step = new TestStep();
        step.InitializeRuntimeState(_hardwareInfo);

        module.TestSteps.Insert(insertIndex, step);

        module.Renumber();

        return step;
    }

    public IReadOnlyList<TestStep> DuplicateSteps(
        TestModule module,
        IEnumerable<TestStep> steps,
        int insertIndex)
    {
        var ordered = steps
            .OrderBy(s => module.TestSteps.IndexOf(s))
            .ToList();

        var clones = new List<TestStep>();

        foreach (var step in ordered)
        {
            step.UpdateDtos();

            var clone = step.Clone();
            clone.InitializeRuntimeState(_hardwareInfo);

            module.TestSteps.Insert(insertIndex++, clone);

            clones.Add(clone);
        }

        module.Renumber();

        return clones;
    }

    public void CopySteps(TestModule module, IEnumerable<TestStep> steps)
    {
        var ordered = steps
            .OrderBy(s => module.TestSteps.IndexOf(s))
            .ToList();

        foreach (var step in ordered)
            step.UpdateDtos();

        _clipboard = ordered
            .Select(step => step.Clone(false))
            .ToList();

        _clipboardIsCut = false;
    }

    public IReadOnlyList<TestStep> PasteSteps(TestModule module, int insertIndex)
    {
        if (!CanPaste)
            return [];

        var pasted = new List<TestStep>();

        foreach (var model in _clipboard!)
        {
            var clone = model.Clone(_clipboardIsCut);
            clone.InitializeRuntimeState(_hardwareInfo);

            module.TestSteps.Insert(insertIndex++, clone);

            pasted.Add(clone);
        }

        module.Renumber();

        if (_clipboardIsCut)
        {
            _clipboard = null;
            _clipboardIsCut = false;
        }

        return pasted;
    }

    public IReadOnlyList<TestStep> CutSteps(TestModule module, IEnumerable<TestStep> steps)
    {
        var ordered = steps
            .OrderBy(s => module.TestSteps.IndexOf(s))
            .ToList();

        if (ordered.Count == 0)
            return [];

        foreach (var step in ordered)
            step.UpdateDtos();

        _clipboard = ordered
            .Select(step => step.Clone(preserveId: true))
            .ToList();

        _clipboardIsCut = true;

        foreach (var step in ordered.OrderByDescending(s => module.TestSteps.IndexOf(s)))
            module.TestSteps.Remove(step);

        module.Renumber();

        return ordered;
    }

    public int RemoveSteps(TestModule module, IEnumerable<TestStep> steps)
    {
        var ordered = steps
            .OrderBy(s => module.TestSteps.IndexOf(s))
            .ToList();

        if (ordered.Count == 0)
            return -1;

        var selectionIndex = module.TestSteps.IndexOf(ordered.First());

        foreach (var step in ordered.AsEnumerable().Reverse())
            module.TestSteps.Remove(step);

        module.Renumber();

        if (module.TestSteps.Count == 0)
            return -1;

        return Math.Min(selectionIndex, module.TestSteps.Count - 1);
    }

    public bool MoveStepUp(TestModule module, TestStep step)
    {
        var index = module.TestSteps.IndexOf(step);

        if (index <= 0)
            return false;

        module.TestSteps.Move(index, index - 1);

        module.Renumber();

        return true;
    }

    public bool MoveStepDown(TestModule module, TestStep step)
    {
        var index = module.TestSteps.IndexOf(step);

        if (index < 0 || index >= module.TestSteps.Count - 1)
            return false;

        module.TestSteps.Move(index, index + 1);

        module.Renumber();

        return true;
    }
}