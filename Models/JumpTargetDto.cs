namespace ATLab.Models;

public class JumpTargetDto
{
    public string Id { get; }
    public int Number { get; }
    public string Name { get; }
    
    public string Display => $"{Number}: {Name}";

    public JumpTargetDto(string id, int number, string name)
    {
        Id = id;
        Number = number;
        Name = name;
    }
}