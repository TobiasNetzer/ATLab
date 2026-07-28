using System;
using System.Collections.Generic;
using System.Linq;
using ATLab.Interfaces;
using ATLab.Models;

namespace ATLab.Services;

public class TestStepEditor : ITestStepEditor
{
    private readonly IHardwareInfo _hardwareInfo;
    private readonly ProjectModel _projectModel;

    private List<TestStep>? _clipboard;
    private bool _clipboardIsCut;
    
    public bool CanPaste => _clipboard != null && _clipboard.Count > 0;

    public TestStepEditor(
        IHardwareInfo hardwareInfo,
        ProjectModel projectModel)
    {
        _hardwareInfo = hardwareInfo;
        _projectModel = projectModel;
    }

    public TestStep AddStep(int insertIndex)
    {
        var step = new TestStep();

        _projectModel.TestSteps.Insert(insertIndex, step);

        Renumber();

        return step;
    }

    public IReadOnlyList<TestStep> DuplicateSteps(
        IEnumerable<TestStep> steps,
        int insertIndex)
    {
        var ordered = steps
            .OrderBy(s => _projectModel.TestSteps.IndexOf(s))
            .ToList();

        var clones = new List<TestStep>();

        foreach (var step in ordered)
        {
            step.UpdateDtos();

            var clone = step.Clone();

            _projectModel.TestSteps.Insert(insertIndex++, clone);

            clones.Add(clone);
        }

        Renumber();

        return clones;
    }

    public void CopySteps(IEnumerable<TestStep> steps)
    {
        var ordered = steps
            .OrderBy(s => _projectModel.TestSteps.IndexOf(s))
            .ToList();

        foreach (var step in ordered)
            step.UpdateDtos();

        _clipboard = ordered
            .Select(step => step.Clone(false))
            .ToList();

        _clipboardIsCut = false;
    }

    public IReadOnlyList<TestStep> PasteSteps(int insertIndex)
    {
        if (!CanPaste)
            return [];

        var pasted = new List<TestStep>();

        foreach (var model in _clipboard!)
        {
            var clone = model.Clone(_clipboardIsCut);

            _projectModel.TestSteps.Insert(insertIndex++, clone);

            pasted.Add(clone);
        }

        Renumber();

        if (_clipboardIsCut)
        {
            _clipboard = null;
            _clipboardIsCut = false;
        }

        return pasted;
    }
    
    public IReadOnlyList<TestStep> CutSteps(IEnumerable<TestStep> steps)
    {
        var ordered = steps
            .OrderBy(s => _projectModel.TestSteps.IndexOf(s))
            .ToList();

        if (ordered.Count == 0)
            return [];

        foreach (var step in ordered)
            step.UpdateDtos();

        _clipboard = ordered
            .Select(step => step.Clone(preserveId: true))
            .ToList();

        _clipboardIsCut = true;

        foreach (var step in ordered.OrderByDescending(s => _projectModel.TestSteps.IndexOf(s)))
            _projectModel.TestSteps.Remove(step);

        Renumber();

        return ordered;
    }

    public int RemoveSteps(IEnumerable<TestStep> steps)
    {
        var ordered = steps
            .OrderBy(s => _projectModel.TestSteps.IndexOf(s))
            .ToList();

        if (ordered.Count == 0)
            return -1;

        var selectionIndex = _projectModel.TestSteps.IndexOf(ordered.First());

        foreach (var step in ordered.AsEnumerable().Reverse())
            _projectModel.TestSteps.Remove(step);

        Renumber();

        if (_projectModel.TestSteps.Count == 0)
            return -1;

        return Math.Min(selectionIndex, _projectModel.TestSteps.Count - 1);
    }

    public bool MoveStepUp(TestStep step)
    {
        var index = _projectModel.TestSteps.IndexOf(step);

        if (index <= 0)
            return false;

        _projectModel.TestSteps.Move(index, index - 1);

        Renumber();

        return true;
    }

    public bool MoveStepDown(TestStep step)
    {
        var index = _projectModel.TestSteps.IndexOf(step);

        if (index < 0 || index >= _projectModel.TestSteps.Count - 1)
            return false;

        _projectModel.TestSteps.Move(index, index + 1);

        Renumber();

        return true;
    }

    private void Renumber()
    {
        for (var i = 0; i < _projectModel.TestSteps.Count; i++)
            _projectModel.TestSteps[i].Number = i + 1;
    }
}