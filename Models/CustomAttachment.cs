namespace ATLab.Models;

public class CustomAttachment
{
    public string Path { get; init; } = string.Empty;
    public string FileName => System.IO.Path.GetFileName(Path);
}