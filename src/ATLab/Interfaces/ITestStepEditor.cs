using System.Collections.Generic;
using ATLab.Models;

namespace ATLab.Interfaces;

/// <summary>Editing operations on the steps of a module, including the step clipboard.</summary>
public interface ITestStepEditor
{
    bool CanPaste { get; }
    TestStep AddStep(TestModule module, int insertIndex);
    IReadOnlyList<TestStep> DuplicateSteps(TestModule module, IEnumerable<TestStep> steps, int insertIndex);
    void CopySteps(TestModule module, IEnumerable<TestStep> steps);
    IReadOnlyList<TestStep> PasteSteps(TestModule module, int insertIndex);
    IReadOnlyList<TestStep> CutSteps(TestModule module, IEnumerable<TestStep> steps);
    int RemoveSteps(TestModule module, IEnumerable<TestStep> steps);
    bool MoveStepUp(TestModule module, TestStep step);
    bool MoveStepDown(TestModule module, TestStep step);
}