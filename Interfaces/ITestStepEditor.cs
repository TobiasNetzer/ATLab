using System.Collections.Generic;
using ATLab.Models;
using ATLab.ViewModels;

namespace ATLab.Interfaces;

public interface ITestStepEditor
{
    bool CanPaste { get; }
    TestStep AddStep(int insertIndex);
    TestStepViewModel CreateViewModel(TestStep step);
    IReadOnlyList<TestStep> DuplicateSteps(IEnumerable<TestStep> steps, int insertIndex);
    void CopySteps(IEnumerable<TestStep> steps);
    IReadOnlyList<TestStep> PasteSteps(int insertIndex);
    IReadOnlyList<TestStep> CutSteps(IEnumerable<TestStep> steps);
    int RemoveSteps(IEnumerable<TestStep> steps);
    bool MoveStepUp(TestStep step);
    bool MoveStepDown(TestStep step);
}