using ATLab.Models;

namespace ATLab.Interfaces;

public interface ITestModuleEditor
{
    TestModule AddModule(int insertIndex);
    TestModule DuplicateModule(TestModule module);
    int RemoveModule(TestModule module);
    bool MoveModuleUp(TestModule module);
    bool MoveModuleDown(TestModule module);
}