namespace ATLab.Models;

/// <summary>Entry of the jump target list of the pass/fail actions.</summary>
public class JumpTargetDto
{
    // Entry at the top of the jump target list to clear the target
    public static JumpTargetDto None { get; } = new(string.Empty, "—");

    public string Id { get; }

    public string Display { get; }

    public JumpTargetDto(string id, string moduleName, int number, string name)
        : this(id, $"{moduleName} / {number}: {name}")
    {
    }

    private JumpTargetDto(string id, string display)
    {
        Id = id;
        Display = display;
    }
}