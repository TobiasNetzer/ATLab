namespace ATLab.Models;

public class JumpTargetDto
{
    public string Id { get; }
    private int Number { get; }
    private string Name { get; }
    
    public string Display => $"{Number}: {Name}";

    public JumpTargetDto(string id, int number, string name)
    {
        Id = id;
        Number = number;
        Name = name;
    }
}