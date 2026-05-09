using ATLab.ViewModels;

namespace ATLab.Interfaces;

public interface ITestStepEditor
{
    bool HasClipboard { get; }
    void AddStep(TestingTabViewModel vm);
    void DuplicateSteps(TestingTabViewModel vm);
    void CopySteps(TestingTabViewModel vm);
    void PasteSteps(TestingTabViewModel vm);
    void CutSteps(TestingTabViewModel vm);
    void RemoveSteps(TestingTabViewModel vm);
    void MoveStepUp(TestingTabViewModel vm);
    void MoveStepDown(TestingTabViewModel vm);
    int ComputeInsertIndex(TestingTabViewModel vm);
    void Renumber(TestingTabViewModel vm);
}