namespace ATLab.Models;

public class JumpTargetDto
{
    public string Id { get; }
    private string ModuleName { get; }
    private int Number { get; }
    private string Name { get; }

    public string Display => $"{ModuleName} / {Number}: {Name}";

    public JumpTargetDto(string id, string moduleName, int number, string name)
    {
        Id = id;
        ModuleName = moduleName;
        Number = number;
        Name = name;
    }
}