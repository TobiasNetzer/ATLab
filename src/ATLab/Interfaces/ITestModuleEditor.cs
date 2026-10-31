using ATLab.Models;

namespace ATLab.Interfaces;

/// <summary>Editing operations on the modules of the project.</summary>
public interface ITestModuleEditor
{
    TestModule AddModule(int insertIndex);
    TestModule DuplicateModule(TestModule module);
    int RemoveModule(TestModule module);
    bool MoveModuleUp(TestModule module);
    bool MoveModuleDown(TestModule module);
}