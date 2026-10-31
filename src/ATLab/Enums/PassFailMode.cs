namespace ATLab.Enums;

/// <summary>What happens after a step passed or failed.</summary>
public enum PassFailMode
{
    CONTINUE,
    REPEAT,
    END_TEST,
    JUMP_TO,
    SHOW_COMMENT
}