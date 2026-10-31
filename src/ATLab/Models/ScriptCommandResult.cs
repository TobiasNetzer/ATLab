namespace ATLab.Models;

/// <summary>
/// What happened to one command of a script run: the response and, for a command with a response check, its
/// outcome. Filled in by the command executor while the command runs.
/// </summary>
public class ScriptCommandResult
{
    /// <summary>Position of the command in the script, starting at 1.</summary>
    public int Number { get; init; }

    /// <summary>Command text after the script variables are applied.</summary>
    public string Command { get; init; } = string.Empty;

    public bool IsExpectResponse { get; init; }
    public bool IsEvaluate { get; init; }
    public bool IsResponseChecked { get; init; }

    /// <summary>Received response, null if the command was only written or no response arrived.</summary>
    public byte[]? Response { get; set; }

    /// <summary>Processed value of the response check, null if the response could not be processed.</summary>
    public string? CheckValue { get; set; }

    public bool? IsCheckPassed { get; set; }

    /// <summary>Why the response check failed.</summary>
    public string? CheckMessage { get; set; }

    public string FailureText => $"Command {Number} '{Command}': {CheckMessage}";
}